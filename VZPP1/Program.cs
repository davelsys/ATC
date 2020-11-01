using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.SqlClient;

enum ReqType { Unknown,NewPayment,ConfirmPayment };


namespace VZPP1
{
    class Program
    {
        static System.Net.NetworkCredential cred = new System.Net.NetworkCredential("YGsaySYS", "rco$87API");

        static void xMain(string[] args)
        {
           // AddFunds("52555537600001", "22", "30027940");
        }

        static Int32 AddFunds(int Reqid,SqlDataReader rdr,ref Int32 VendTransId, ref String ErrMsg)
        {
            Int32 OfferingId = 30101926;// TEST 30027940;
            Int32 Amt = 30;

            DollarPhone.PinManager pm = new DollarPhone.PinManager();
            DollarPhone.TopUpReqType tureq = new DollarPhone.TopUpReqType();
      

            tureq.Action = DollarPhone.TopUpAction.AddFunds;
            tureq.OrderId = Reqid.ToString();
            tureq.OfferingId = OfferingId;
            tureq.PhoneNumber = DBString(rdr,"MDN");
            tureq.Amount = Amt;
       
            pm.Credentials = cred;

            DollarPhone.TopUpResp turesp = pm.TopUpRequest(tureq);

            int rc = turesp.responseCode;

            if (rc < 1)
            {
                if (turesp.responseMessage != null)
                    ErrMsg = turesp.responseMessage;
            }
            else
                VendTransId = (Int32)turesp.TransId;

            return rc;
        }
        static Int32 ConfirmFunds(int Reqid, SqlDataReader rdr, ref Int32 VendTransId, ref String ErrMsg)
        {
            DollarPhone.PinManager pm = new DollarPhone.PinManager();
            DollarPhone.TransResponseType trresp = new DollarPhone.TransResponseType();
            pm.Credentials = cred;

            trresp = pm.TopupConfirm(VendTransId);
            int rc = 1;
            switch (trresp.Status)
            {
                case DollarPhone.TransactionStatus.Success:
                    rc = 1;
                    break;
                case DollarPhone.TransactionStatus.Failed:
                    if (trresp.ErrorMsg != null)
                        ErrMsg = trresp.ErrorMsg;
                    rc = -1;
                    break;
                case DollarPhone.TransactionStatus.Pending:
                    rc = 0;
                    break;
            }
            return rc;
        }

        static void UpdateCreditLimit(SqlConnection sqlconn)
        {
            try
            { 
                DollarPhone.PinManager pm = new DollarPhone.PinManager();
                pm.Credentials = cred;
                DollarPhone.AgentCreditLimit cl = pm.GetAgentCreditLimit();
               

                SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.Connection = sqlconn;
           
                sqlCommand.CommandText =String.Format("Update ppc.dbo.VZPPBalance set Balance = {0},  UpdateDate = getdate()", cl.AvailableCredit);
                
                VZPPSvc.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                VZPPSvc.Log("Error Updating CreditLimit - " + ((object)ex.Message).ToString());
            }
        }


        private static SqlConnection sqlconn;

        static Svc VZPPSvc = new Svc();
      
        static void Main()//string[] args
        {

            VZPPSvc.OpenLog("C:\\VZPP1\\VZPPLog.txt");

            DB db = new DB();
            db.OpenDB("VZPP", "sa3", "davel");

            sqlconn = db.sqlconn;

 
            SqlCommand sqlCommand = new SqlCommand(string.Format("VZPPGetOpenReq", new object[0]), Program.sqlconn);
            string[] args = new string[50];

            Console.WriteLine();
            Console.Write("\nProcessing");

            try
            {
                while (true)
                {
                    SqlDataReader sqlDr = sqlCommand.ExecuteReader();

                    Console.Write(".");
                    if (sqlDr.HasRows)
                    {
                        while (sqlDr.Read())
                        {
                            /*
                               VZPPSvc.Log("\n=======================================");


                               for (int index = 0; index < sqlDr.FieldCount; ++index)
                               {
                                   args[index] = sqlDr[index].ToString();
                                   VZPPSvc.Log(sqlDr.GetName(index) + "=" + sqlDr[index] + " ");
                               }

                               VZPPSvc.Log("\n===");
                            */

                            int reqid = DBInt32(sqlDr, "VZPPReqId");
                            bool pollreq = DBBool(sqlDr, "Poll");

                            ReqType reqtype = (ReqType)ReqType.Parse(typeof(ReqType), DBString(sqlDr, "ReqType"), true);
                            DateTime now = DateTime.Now;

                            int reqrc = VERZPPRequest(reqid, reqtype, sqlconn, sqlDr, now);

                            VZPPSvc.Log(string.Format("Reqid={0}, MDN={1} ,PollCount={2}, Result={3}", reqid.ToString(), DBString(sqlDr, "MDN"), DBInt32(sqlDr, "PollCount"), (object)reqrc.ToString()));
                        }
                    }

                    sqlDr.Close();
                    Console.Write(".");
                    Thread.Sleep(1000 * 30);
                }
            }

            catch (Exception ex)
            {
                if (ex != null)
                {
                    VZPPSvc.Log("****Main ERROR MSG: " + ex.Message);
                    VZPPSvc.Log("****Main ERROR Trace: " + ex.StackTrace);
                }

            }
            db.CloseDB();
        }



        static int VERZPPRequest(int reqid, ReqType reqType, SqlConnection sqlconn, SqlDataReader rdr, DateTime dt)
        {

            int planid = DBInt32(rdr, "PlanId");
            int orderid = DBInt32(rdr, "OrderId");

            Int32 VendTransId = DBInt32(rdr, "VendTransId");

            string esn = DBString(rdr, "ESN");
            string mdn = DBString(rdr, "MDN");
            bool polling = DBBool(rdr, "Poll");
            int pollcount = DBInt32(rdr, "PollCount");

            String ErrMsg = "";


            int rc = -1;
            try
            {
                if (VendTransId < 1)
                    rc = AddFunds(reqid, rdr, ref VendTransId, ref ErrMsg);
                else
                if (polling)
                   rc = ConfirmFunds(reqid, rdr, ref VendTransId, ref ErrMsg);              

                if (rc < 0)
                    CloseReq(reqid, sqlconn, rc, ErrMsg);
                else
                {
                    if (polling && rc == 1)
                    {
                        CloseReq(reqid, sqlconn, rc, "Payment Confirmed");
                        UpdateCreditLimit(sqlconn);
                    }
                    else
                    if (pollcount > 6) // 180 seconds = (6 * 30)
                        CloseReq(reqid, sqlconn, rc, "Poll Limit Exceeded");
                    else
                        UpdatePoll(reqid, 1, VendTransId, sqlconn);
                }
            }

            catch (Exception e)
            {
                VZPPSvc.Log("****VZPPReq ERROR MSG: " + e.Message);
                CloseReq(reqid, sqlconn, e.HResult, e.Message);
            }

            return rc;
        }


        private static void CloseReq(int reqid, SqlConnection sqlconn, int rc, string errormsg = "")
        {
            if (errormsg == null)
                errormsg = "";

            if (errormsg.Length > 50)
            {
                errormsg = errormsg.Substring(0, 50);
            }

            if (errormsg.IndexOf("'") > -1)
            {
                errormsg = errormsg.Replace("'", "");
            }


            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = "Update VZPPReq set processed = GETDATE(),respStatus='" + rc.ToString() + "' ";
                sqlCommand.CommandText += ", RespAckMsg = '" + errormsg + "' ";
                sqlCommand.CommandText += ", Poll=0 ";
                sqlCommand.CommandText += " where VZPPReqId= " + reqid + "; ";
                if (errormsg.Length > 1)
                {
                    sqlCommand.CommandText += " Update ppc.dbo.orders set statusmsg = '" + errormsg + " - " + rc.ToString() + "', update_date = getdate() where order_id = (select orderid from VZPPReq where VZPPreqid = " + reqid + "); ";
                    sqlCommand.CommandText += " Exec ppc.dbo.VZPP_UpdateVZPPMdn " + reqid + "; ";

                }
                VZPPSvc.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                VZPPSvc.Log("Error closing VERZPP Req - " + ((object)ex.Message).ToString());
            }
        }

        private static void UpdatePoll(int reqid, int Status, int VendTransId, SqlConnection sqlconn)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = string.Format("Update VZPPReq set poll={0},LastPoll=GETDATE(),VendTransId={1},PollCount=IsNull(PollCount,0) + 1 where VZPPReqId={2}", (Status < 1) ? 0 : 1,VendTransId,reqid);
                VZPPSvc.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                VZPPSvc.Log("Error updating VERZPPReq Poll - " + ((object)ex.Message).ToString());
            }
        }

        static void UpdatePON(int reqid, string PON, SqlConnection sqlconn)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = string.Format("Update VERZPPReq set PON={0} where VZPPReqId={1}", PON, reqid);
                VZPPSvc.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                VZPPSvc.Log("Error updating VERZPPReq PON - " + ((object)ex.Message).ToString());
            }
        }

        /*
        private static void ProcessResponse(int reqId, string mdn, string status)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = string.Format(" exec ppc.dbo.verzpp_resp {0}, '{1}', '{2}' ", reqId, mdn, status);
                VZPPSvc.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                VZPPSvc.Log("ProcessResponse Error - " + ((object)ex.Message).ToString());
            }
        }
        */

        static String DBString(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return rdr.GetString(offset);
            return "";
        }

        static Int16 DBInt16(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return (Int16)rdr.GetSqlInt16(offset);
            return 0;
        }

        static Int32 DBInt32(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return (Int32)rdr.GetSqlInt32(offset);
            return 0;
        }

        static Int64 DBInt64(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return (Int64)rdr.GetSqlInt64(offset);
            return 0;
        }
        static Boolean DBBool(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return (Boolean)rdr.GetSqlBoolean(offset);
            return false;
        }
    }
}
    
