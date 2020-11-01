using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.Configuration;


enum ReqType { Unknown, NewService, DisconnectOrder, Restore, PortService, ChangeESN, Suspend, OrderInquiry, MDNInquiry };
enum MDNStatus { Unknown, Inactive, Active, Suspended, Disconnected, Hotlined, Rejected };

namespace TelcoService
{
    class Program
    {

        static Telco.MdnServicesSoapClient telcoSvc = new Telco.MdnServicesSoapClient();
        static Telco.Port telcoPort = new Telco.Port();
        static Telco.TerminationInfo telcoTI;
        static Telco.Info telcoInfo = new Telco.Info();



        static String user = ConfigurationManager.AppSettings["telco_User"]; //"TSP_AGENTS_LITECALL";
        static string pwd = ConfigurationManager.AppSettings["telco_PWD"]; //"buBeh3st";
        //static string account = "1166";

        static Svc telcoService = new Svc();

        //enum ReqType { NewService, OrderInquiry, PortValidation, ValidationInquiry };

        private static bool prodmode;
        private static bool pollreq;
        private static int reqid;
        private static SqlConnection sqlconn;

        static void Main()//string[] args
        {

            telcoService.OpenLog("C:\\Telco\\TelcoLog.txt");

            DB db = new DB();
            db.OpenDB("Telco", "sa3", "davel");

            sqlconn = db.sqlconn;

            SqlCommand sqlCommand = new SqlCommand(string.Format("TelGetOpenReq", new object[0]), Program.sqlconn);
            string[] args = new string[50];
            //string PON = "";

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
                            telcoService.Log("\n=======================================");
                            for (int index = 0; index < sqlDr.FieldCount; ++index)
                            {
                                args[index] = sqlDr[index].ToString();
                                telcoService.Log(sqlDr.GetName(index) + "=" + sqlDr[index] + " ");

                            }
                            telcoService.Log("\n===");

                            int reqid = (int)sqlDr["TelreqId"];
                            bool pollreq = DBBool(sqlDr, "Poll");
                            string PON = DBString(sqlDr, "PON");

                            if ((int)PON.Length < 1)
                            {
                                PON = reqid.ToString();
                                UpdatePON(reqid, PON, sqlconn);
                            }

                            ReqType reqtype = (ReqType)ReqType.Parse(typeof(ReqType), DBString(sqlDr, "ReqType"), true);
                            DateTime now = DateTime.Now;

                            if (pollreq)
                            {
                                switch (reqtype)
                                {
                                    //case ReqType.NewService:
                                    case ReqType.ChangeESN:
                                        reqtype = ReqType.MDNInquiry;
                                        break;
                                    case ReqType.PortService:
                                        reqtype = ReqType.MDNInquiry;
                                        break;
                                    case ReqType.NewService:
                                        reqtype = ReqType.OrderInquiry;
                                        break;
                                }
                            }

                            int reqrc = TelcoRequest(reqid, reqtype, PON, sqlDr, now);

                            telcoService.Log(string.Format("Reqid = {0}, Req={1}, Result = {2}", reqid.ToString(), reqtype.ToString(), (object)reqrc.ToString()));
                        }
                    }
                    else
                    {
                        Console.Write(".");
                        Thread.Sleep(1000 * 30);
                    }

                    sqlDr.Close();
                }

            }
            catch (Exception ex)
            {
                telcoService.Log("****Main ERROR MSG: " + ex.Message);
                telcoService.Log("****Main ERROR Trace: " + ex.StackTrace);

            }
            db.CloseDB();
        }

        static void UpdatePON(int reqid, string PON, SqlConnection sqlconn)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = string.Format("Update TelcoReq set PON={0} where TelReqId={1}", PON, reqid);
                telcoService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                telcoService.Log("Error updating TelcoReq PON - " + ((object)ex.Message).ToString());
            }
        }

        static int TelcoRequest(int reqid, ReqType reqType, string PON, SqlDataReader rdr, DateTime dt)
        {
            string refnum = DBString(rdr, "VendRef");
            if (refnum.Length < 1)
                refnum = PON;
            int vendref = Convert.ToInt32(refnum);
            //int planid = DBInt32(rdr, "PlanId");
            int orderid = DBInt32(rdr, "OrderId");

            string esn = DBString(rdr, "ESN");
            string mdn = DBString(rdr, "MDN");

            string zip = DBString(rdr, "NPAZip");
            string icc = "";

           // int payid = planid;
            int cycle = 1;

            Telco.Wireless wrc;

            string callbackurl = "";
            //string respmsg = "";
            bool hasError = false;
            string errorMsg = "";

            int rc = -1;
            Telco.VbosPackage vbPackage = new Telco.VbosPackage();
            vbPackage.PackageName = "Litecall - Pay Per Use Voice Plan";
            vbPackage.MRC = 2;
            vbPackage.InstallDate = DateTime.Now;

            Telco.VbosPackage[] arrPackage = new Telco.VbosPackage[1];


            Telco.VbosPackage vbPackageEmpty = new Telco.VbosPackage();
            vbPackageEmpty.InstallDate = default(DateTime);
            arrPackage[0] = vbPackageEmpty;

            try
            {


                switch (reqType)
                {
                    case ReqType.NewService:
                        telcoInfo.FullName = DBString(rdr, "FName") + " " + DBString(rdr, "LName");
                        telcoInfo.HomeAddress1 = DBString(rdr, "Address");
                        telcoInfo.HomeCity = DBString(rdr, "City");
                        telcoInfo.HomeState = DBString(rdr, "State");
                        telcoInfo.HomeZip = DBString(rdr, "Zip");
                        telcoInfo.ShipAddress1 = DBString(rdr, "Address");
                        telcoInfo.ShipCity = DBString(rdr, "City");
                        telcoInfo.ShipState = DBString(rdr, "State");
                        telcoInfo.ShipZip = DBString(rdr, "Zip");
                        telcoInfo.BillingCycle = "23";
                        telcoInfo.ShipName = DBString(rdr, "FName") + " " + DBString(rdr, "LName");


                       telcoSvc.CreateCustomer(user, pwd, ref telcoInfo);
                        telcoService.Log(String.Format("Request: NewService, CreateCustomer - user={0},pwd={1},name={2}, address={3},zip={4}, billingcycle=23", user, pwd, DBString(rdr, "FName") + " " + DBString(rdr, "LName"), DBString(rdr, "Address") + " " + DBString(rdr, "City") + ", " + DBString(rdr, "State"), DBString(rdr, "Zip")));
                        //telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, name={3}, Account={4}", ReqType.GetName(typeof(ReqType), reqType), user, pwd, DBString(rdr, "FName") + " " + DBString(rdr, "LName"), telcoInfo.AccountNumber));

                        telcoService.Log("Request: NewService, VerizonPostPaid_Activate - AcctNumber=" + telcoInfo.AccountNumber + ", NPAZIP= " + DBString(rdr, "NPAZip") + ", ESN=" + esn + ", icc=" + icc);
                       wrc = telcoSvc.VerizonPostPaid_Activate(user, pwd, telcoInfo.AccountNumber, DBString(rdr, "NPAZip"), esn, icc, "LITECALL_VZ", default(DateTime), default(DateTime), "Jack Greenberg",
                            vbPackage, vbPackageEmpty, vbPackageEmpty, vbPackageEmpty, vbPackageEmpty, vbPackageEmpty, arrPackage, arrPackage, refnum, callbackurl);
                        UpdateRequest(reqid, wrc, rdr, "");

                        rc = 1; //SG 12/05/16 need to poll - to call get wireless - to get MDN
                        break;
                    case ReqType.DisconnectOrder:
                        telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, mdn={3}", ReqType.GetName(typeof(ReqType), reqType), user, pwd, mdn));
                        wrc = telcoSvc.DisconnectMDN(user, pwd, mdn);

                        if (int.Parse(wrc.Status) != 1) // SG if status is not Inactive
                        {
                            UpdateRequest(reqid, wrc, rdr);

                            CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)), "Disconnect Successful");
                            rc = 0;
                            ProcessResponse(reqid, mdn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)));
                            //UpdateMDN(orderid, wrc);
                        }
                        else
                        {
                            if (DBInt32(rdr, "PollCount") > 500)
                            {
                                //SG stop polling after 500 attemps and set msg to error
                                CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), rc), "Disconnect Incomplete");
                            }
                            else
                            {
                                rc = 1;
                            }
                        }
                        break;
                    case ReqType.ChangeESN:
                        telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, mdn={3}, esn={4}", ReqType.GetName(typeof(ReqType), reqType), user, pwd, mdn, esn));
                        wrc = telcoSvc.ESNSwap(user, pwd, mdn, esn);
                        if (int.Parse(wrc.Status) != 1) // SG if status is not Inactive
                        {
                            UpdateRequest(reqid, wrc, rdr);

                            CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)), "Change ESN Successful");
                            rc = 0;
                            ProcessResponse(reqid, mdn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)));
                            //UpdateMDN(orderid, wrc);
                        }
                        else
                        {
                            if (DBInt32(rdr, "PollCount") > 500)
                            {
                                //SG stop polling after 500 attemps and set msg to error
                                CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), rc), "Change ESN Incomplete");
                            }
                            else
                            {
                                rc = 1;
                            }
                        }
                        break;
                    case ReqType.PortService:

                        //telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, esn={3}, call={4},  cycle={5}, callbackurl={6}",
                                       //ReqType.GetName(typeof(ReqType), reqType), user, pwd, esn, "CreateCustomer", cycle.ToString(),  callbackurl));

                        telcoInfo.FullName = DBString(rdr, "FName") + " " + DBString(rdr, "LName");
                        telcoInfo.HomeAddress1 = DBString(rdr, "Address");
                        telcoInfo.HomeCity = DBString(rdr, "City");
                        telcoInfo.HomeState = DBString(rdr, "State");
                        telcoInfo.HomeZip = DBString(rdr, "Zip");
                        telcoInfo.ShipAddress1 = DBString(rdr, "Address");
                        telcoInfo.ShipCity = DBString(rdr, "City");
                        telcoInfo.ShipState = DBString(rdr, "State");
                        telcoInfo.ShipZip = DBString(rdr, "Zip");
                        telcoInfo.BillingCycle = "23";
                        telcoInfo.ShipName = DBString(rdr, "FName") + " " + DBString(rdr, "LName");



                        telcoSvc.CreateCustomer(user, pwd, ref telcoInfo);
                        telcoService.Log(String.Format("Request: Port, CreateCustomer - user={0}, pwd={1}, fullname={2}, address={3}, zip={4}, billingcycle=23", user, pwd, DBString(rdr, "FName") + " " + DBString(rdr, "LName"), DBString(rdr, "Address") + " " + DBString(rdr, "City") + ", " + DBString(rdr, "State"), DBString(rdr, "Zip")));
                        //telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, esn={3}, icc={4}, cycle={5}, account={6}, callbackurl={7}",
                                       //ReqType.GetName(typeof(ReqType), reqType), user, pwd, esn, icc,  cycle.ToString(), telcoInfo.AccountNumber, callbackurl));

                        String addr = DBString(rdr, "Address");
                        String[] addrarr = addr.Split(' ');

                        telcoPort.Id = 0;
                        telcoPort.OrderId = 0;
                        telcoPort.AccountNumber = mdn;
                        telcoPort.OldCarrier = "VPS"; //is this always VPS?
                        telcoPort.OldCarrierAccountNumber = DBString(rdr, "PORTACCT");;
                        telcoPort.SSN = "";
                        telcoPort.FirstName = DBString(rdr, "FName");
                        telcoPort.MiddleName = "";
                        telcoPort.LastName = DBString(rdr, "LName");
                        telcoPort.BusinessName = "";
                        telcoPort.Password = DBString(rdr, "PORTPASSWD");
                        telcoPort.Address1 = addr; //.Substring(addrarr[0].Length + 1); //SG 01/26/17 removed send the whole address
                        telcoPort.Address2 = "";
                        telcoPort.City = DBString(rdr, "City");
                        telcoPort.State = DBString(rdr, "State");
                        telcoPort.Zip = DBString(rdr, "Zip");
                        telcoPort.RequestedDDT = ""; //"09/08/2016"; leave empty?
                        telcoPort.MDN = mdn;
                        telcoPort.Notes = "";
                        telcoPort.StreetNumber = addrarr[0].ToString();


                        //SG 01/26/17 log info that we send on port
                        telcoService.Log("Request: NewPort, Verizon_ActivatePort2 - user=" + user + ", pwd=" + pwd + ", esn=" + esn + ", icc=" + icc + ", telcoAcctNumber=" + telcoInfo.AccountNumber);
                        telcoService.Log("Request: NewPort, Verizon_ActivatePort2, *PortInfo* - acctnumber=" + DBString(rdr, "PORTACCT") + ", OldCarrier=vsp, OldCarrierAccountNumber=" + mdn + ", fname=" + DBString(rdr, "FName") + ", lname=" + DBString(rdr, "LName") + ", Password=" + DBString(rdr, "PORTPASSWD"));
                        telcoService.Log(", Address1=" + addr + ", city=" + DBString(rdr, "City") + ", state=" + DBString(rdr, "State") + ", zip=" + DBString(rdr, "Zip") + ", mdn=" + mdn + ", streetnumber=" + addrarr[0].ToString());


                       telcoSvc.Verizon_ActivatePort2(user, pwd, mdn, esn, icc, telcoInfo.AccountNumber, "LITECALL_VZ", default(DateTime), default(DateTime), "", telcoPort, vbPackage,
                          vbPackageEmpty, vbPackageEmpty, vbPackageEmpty, vbPackageEmpty, vbPackageEmpty, arrPackage, arrPackage, ref hasError, ref errorMsg);
                        rc = 1;
                        break;
                    case ReqType.OrderInquiry:
                        wrc = telcoSvc.GetWireless(user, pwd, int.Parse(DBString(rdr, "VendRef")));

                        if (int.Parse(wrc.Status) != 1) // SG if status is not Inactive
                        {
                            UpdateRequest(reqid, wrc, rdr);

                            CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)), "New Service Successful");
                            rc = 0;
                            ProcessResponse(reqid, wrc.MDN, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)));
                            //UpdateMDN(orderid, wrc);
                        }
                        else
                        {
                            if (DBInt32(rdr, "PollCount") > 500)
                            {
                                //SG stop polling after 500 attemps and set msg to error
                                CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), rc), "New Service Incomplete");
                            }
                            else
                            {
                                rc = 1;
                            }
                        }
                        break;
                    case ReqType.MDNInquiry:
                        telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, mdn={3}", ReqType.GetName(typeof(ReqType), reqType), user, pwd, mdn));
                        wrc = telcoSvc.GetWirelessByMDN(user, pwd, mdn);

                        if (int.Parse(wrc.Status) != 1) // SG if status is not Inactive
                        {
                            UpdateRequest(reqid, wrc, rdr);

                            CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)), "Port Successful");
                            rc = 0;
                            ProcessResponse(reqid, mdn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)));
                            //UpdateMDN(orderid, wrc);
                        }
                        else
                        {
                            if (DBInt32(rdr, "PollCount") > 500)
                            {
                                //SG stop polling after 500 attemps and set msg to error
                                CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), rc), "Port Incomplete");
                            }
                            else
                            {
                                rc = 1;
                            }
                        }
                        break;
                    case ReqType.Restore:
                        telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, mdn={3}", ReqType.GetName(typeof(ReqType), reqType), user, pwd, mdn));
                        wrc = telcoSvc.RestoreMDN(user, pwd, mdn);
                        if (int.Parse(wrc.Status) != 1) // SG if status is not Inactive
                        {
                            UpdateRequest(reqid, wrc, rdr);

                            CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)), "Restore Successful");
                            rc = 0;
                            ProcessResponse(reqid, mdn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)));
                            //UpdateMDN(orderid, wrc);
                        }
                        else
                        {
                            if (DBInt32(rdr, "PollCount") > 500)
                            {
                                //SG stop polling after 500 attemps and set msg to error
                                CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), rc), "Restore Incomplete");
                            }
                            else
                            {
                                rc = 1;
                            }
                        }
                        break;
                    case ReqType.Suspend:
                        telcoService.Log(String.Format("Request ({0}): user={1},pwd={2}, mdn={3}", ReqType.GetName(typeof(ReqType), reqType), user, pwd, mdn));
                        //wrc = telcoSvc.SuspendMDN(user, pwd, mdn); //SG 12/14/16 Change this call to Vereizon_Hotline
                        telcoSvc.Verizon_Hotline(user, pwd, mdn, "7188416419");


                        //if (int.Parse(wrc.Status) != 1) // SG if status is not Inactive
                        //{
                            //UpdateRequest(reqid, void, rdr);

                           // CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)), "Suspend Successful");
                            CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), 3), "Suspend Successful");
                            rc = 0;
                            //ProcessResponse(reqid, mdn, MDNStatus.GetName(typeof(MDNStatus), int.Parse(wrc.Status)));
                            ProcessResponse(reqid, mdn, MDNStatus.GetName(typeof(MDNStatus), 3));
                            //UpdateMDN(orderid, wrc);
                        //}
                        //else
                        //{
                        //    if (DBInt32(rdr, "PollCount") > 500)
                        //    {
                        //        //SG stop polling after 500 attemps and set msg to error
                        //        CloseReq(reqid, sqlconn, MDNStatus.GetName(typeof(MDNStatus), rc), "Suspend Incomplete");
                        //    }
                        //    else
                        //    {
                        //        //UpdatePoll(reqid, rc, sqlconn);
                        //        //UpdatePoll(reqid, 1, sqlconn);  //SG keep polling
                        //        rc = 1;
                        //    }
                        //}
                        break;
                    case ReqType.Unknown:
                        rc = 0;
                        break;
                }
            }
            catch (Exception e)
            {
                telcoService.Log("****WRC ERROR MSG: " + e.Message);
                CloseReq(reqid, sqlconn, "Error", e.Message);
                rc = 0;
            }


            UpdatePoll(reqid, rc, sqlconn);

            return rc;
        }

        private static bool UpdateRequest(int reqid, Telco.Wireless wrc, SqlDataReader args, String msg = "")
        {
            String updatestr = "Update TelcoReq set prod=prod";
            String fieldval = "";

            if (wrc != null)
            {
                if (wrc.Id.ToString() != DBString(args, "VendRef"))
                    fieldval += ",VendRef='" + wrc.Id.ToString() + "'";

                if (wrc.MDN != DBString(args, "MDN"))
                    fieldval += ",MDN='" + wrc.MDN + "'";

                if (wrc.ESN != DBString(args, "ESN"))
                    fieldval += ",ESN='" + wrc.ESN + "'";
            }


            if (fieldval.Length == 0)
                return false;

            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = updatestr += fieldval + String.Format(" where Telreqid = {0}", reqid);
                telcoService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                telcoService.Log("Error updating TelcoReq Status - " + ((object)ex.Message).ToString());
                return false;
            }

            //UpdateConMDN(wrc, reqid);

            return true;
        }

        private static void CloseReq(int reqid, SqlConnection sqlconn, string status, string errormsg = "")
        {
            if (errormsg.IndexOf("Server was unable to process request. ---> ") > -1)
            {
                int index = "Server was unable to process request. ---> ".Length - 1;
                errormsg = errormsg.Substring(43);
            } 

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
                sqlCommand.CommandText = "Update TelcoReq set processed = GETDATE(),poll=0,respStatus='" + status + "' ";
                sqlCommand.CommandText += ", RespAckMsg = '" + errormsg + "' ";
                sqlCommand.CommandText += " where telReqId= " + reqid;
                if (errormsg.Length > 1)
                {
                    sqlCommand.CommandText += " Update ppc.dbo.orders set statusmsg = '" + errormsg + " - " + status + "', update_date = getdate() where order_id = (select orderid from telcoreq where telreqid = " + reqid + ")";
                }
                telcoService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                telcoService.Log("Error closing Telco Req - " + ((object)ex.Message).ToString());
            }
        }

        private static void UpdatePoll(int reqid, int Status, SqlConnection sqlconn)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = string.Format("Update telcoReq set poll={0},LastPoll=GETDATE(),PollCount=IsNull(PollCount,0) + 1 where telReqId={1}", (Status < 1) ? 0 : 1, reqid);
                telcoService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                telcoService.Log("Error updating TelcoReq Poll - " + ((object)ex.Message).ToString());
            }
        }

        private static void ProcessResponse(int reqId, string mdn, string status)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = string.Format(" exec ppc.dbo.telco_resp {0}, '{1}', '{2}' ", reqId, mdn, status);
                telcoService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                telcoService.Log("ProcessResponse Error - " + ((object)ex.Message).ToString());
            }
        }

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
