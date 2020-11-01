using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Data.SqlClient;
using System.Configuration;
using System.Net.Http;
using System.Web.Script.Serialization;



namespace SprintService
{
    public class RequestError : Exception
    {
        public RequestError(String message) : base(message) { }
    }
    
    class Program
    {

        static String user = ConfigurationManager.AppSettings["Sprint_Id"];
        static String pwd = ConfigurationManager.AppSettings["Sprint_Key"];

        //static String DEVURL = "https://api-dev.wingalpha.com/api/";
        static String WINGURL = "https://api.wingalpha.com/api/";

        static Svc sprintService = new Svc();
        enum ReqType { NewService, ChangeESN, Port, Poll, Suspend, Restore, Expire};
 
        private static SqlConnection sqlconn;
        static JavaScriptSerializer js = new JavaScriptSerializer();

        static string lastreq = "";

        static void Main(string[] args)
        {
            DB db = new DB();
            db.OpenDB("Sprint", "sa3", "davel");
            sqlconn = db.sqlconn;

            SqlCommand sqlCommand = new SqlCommand(string.Format("SprintGetOpenReq", new object[0]), Program.sqlconn);
            string[] jargs = new string[50];

            Console.WriteLine();
            Console.Write("\nProcessing");

            if (args.Length > 0)  // standalone mode
            {
                Console.WriteLine("args[0]=" + args[0]);

                string jsondata = "";
                string jsonresult = "";

                SqlCommand sqlcmd = new SqlCommand();
                sqlcmd.Connection = sqlconn;
                
                if (String.Compare(args[0], "Test", true) == 0)
                    SendRequest("subscriptions/", "GET", jsondata, ref jsonresult);
                else
                if (String.Compare(args[0], "Synch", true) == 0)
                {
                    SendRequest("subscriptions/", "GET", jsondata, ref jsonresult);
                    SynchSubscriptions(jsonresult,sqlcmd);
                }
                else
                {   
                    SendRequest("subscriptions/" + args[0] + "/", "GET", jsondata, ref jsonresult);
                    var result = js.Deserialize<dynamic>(jsonresult);
                    SynchSub(result,sqlcmd);
                    UpdateRequest(0, ReqType.Poll, args[0], true, jsondata, jsonresult, null);
                }
               // Console.WriteLine("Hit Any key to continue..");
               // Console.ReadKey();
               return;
            }
            sprintService.OpenLog("C:\\Sprint\\SprintLog.txt");

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
                            sprintService.Log("\n=======================================");
                            String logstr = "";
                            for (int index = 0; index < sqlDr.FieldCount; ++index)
                            {
                                jargs[index] = sqlDr[index].ToString();
                                logstr += jargs[index] + "|";
                                //sprintService.Log(sqlDr.GetName(index) + "=" + sqlDr[index] + " ");
                            }
                            sprintService.Log(logstr);
                            sprintService.Log("\n===");

                            int reqid = (int)sqlDr["SprintreqId"];
                            bool pollreq = DBBool(sqlDr, "Poll");

                            ReqType reqtype = (ReqType)ReqType.Parse(typeof(ReqType), DBString(sqlDr, "ReqType"), true);
                            DateTime now = DateTime.Now;

                            bool reqrc;
                            if (pollreq)
                                reqrc = SprintRequest(reqid, ReqType.Poll, sqlDr, now);
                            else
                                reqrc = SprintRequest(reqid, reqtype, sqlDr, now);

                            sprintService.Log(string.Format("Reqid = {0}, Req={1}, Result = {2}", reqid.ToString(), reqtype.ToString(), (object)reqrc.ToString()));
                        }
                    }
                    sqlDr.Close();

                    Console.Write(".");
                    Thread.Sleep(1000 * 30);
                }
             
            }
            catch (Exception ex)
            {
                sprintService.Log("****Main ERROR MSG: " + ex.Message);
                sprintService.Log("****Main ERROR Trace: " + ex.StackTrace);

            }
            db.CloseDB();
        }


        static bool SprintRequest(int reqid, ReqType reqType, SqlDataReader rdr, DateTime dt)
        {
            int planid = DBInt32(rdr, "PlanId");// 10605; 
            int payid = planid;
        
            int orderid = DBInt32(rdr, "OrderId");

            string esn = DBString(rdr, "ESN");
            string mdn = DBString(rdr, "MDN");
            string pin = DBString(rdr, "Pin");
            string subscr_id = DBString(rdr, "SubscrId");
            string npazip = DBString(rdr, "NPAZip");
            string fname = DBString(rdr, "FNAME");
            string lname = DBString(rdr, "LNAME");
            string portacct = DBString(rdr, "PORTACCT");
            string portpasswd = DBString(rdr, "PORTPASSWD");
            
            string jsondata = "";
            string jsonresult = "";
            bool poll = false;
          
            string CSA;
            string npa = "";
            string nxx = "";

            int rc = -1;
                    
            Dictionary<string, object> dict = new Dictionary<string, object>();
            Dictionary<string, string> dict2 = new Dictionary<string, string>();

            sprintService.Log("Reqtype=" +  reqType.ToString());

            switch (reqType)
            {         
                   case ReqType.NewService:
                    if (mdn == null || mdn == "")
                    {
                        try 
                        {
                            if (subscr_id == "")
                            {
                                subscr_id = GetNewSubscription(fname, lname);
                                UpdateSprintReq(reqid, "subscrid", subscr_id);
                            }

                            CSA = GetCSA(npazip, ref npa, ref nxx);

                            dict.Clear();
                            dict.Add("csa", CSA);
                          
                            jsondata = js.Serialize(dict);
                            rc = ModifyCSA(subscr_id, jsondata,ref jsonresult);

                            
                            dict.Add("plan_id", planid.ToString());
                            jsondata = js.Serialize(dict);
                            rc = ModifySubscriptionPlan(subscr_id, jsondata,ref jsonresult);
                            
                            dict.Clear();
                            dict.Add("device_id", esn);

                            jsondata = js.Serialize(dict);
                            rc = ModifySubscriptionDevice(subscr_id, jsondata,ref jsonresult);

                            dict.Clear();
                            dict.Add("pin", pin);
                            dict.Add("npa", npa);
                            jsondata = js.Serialize(dict);
                                 
                            rc = ActivateSubscription(subscr_id, jsondata,ref jsonresult);
                    
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            UpdateRequest(reqid, reqType, subscr_id, poll, jsondata,jsonresult, e.Message);
                            return false;
                        }
                        poll = true;
                        UpdateRequest(reqid, reqType, subscr_id, poll, jsondata, jsonresult, null);
                        return true;
                    }
                    break;
                   

                case ReqType.Port:
                    try
                    { 
                        if (mdn != "")
                        {
                            rc = SendRequest("sprint/validate_port/?mdn=" + mdn, "GET", jsondata,ref jsonresult);

                            if (rc == 200)
                            {
                                dynamic dobj = js.Deserialize<dynamic>(jsonresult);
                                string carrier = dobj["carrier"];


                                if (subscr_id == "")
                                {
                                    subscr_id = GetNewSubscription(fname,lname);
                                    UpdateSprintReq(reqid, "subscrid", subscr_id);
                                }


                                dict.Clear();
                                dict.Add("plan_id", planid.ToString());
                           
                                jsondata = js.Serialize(dict);
                                rc = ModifySubscriptionPlan(subscr_id, jsondata,ref jsonresult);

                                sprintService.Log(jsonresult);

                                dict.Clear();
                                dict.Add("device_id", esn);
                                jsondata = js.Serialize(dict);
                                rc = ModifySubscriptionDevice(subscr_id, jsondata,ref jsonresult);
                                    //Console.WriteLine(jsonresult);

                             
                               
                                string address = DBString(rdr, "ADDRESS");
                                string city = DBString(rdr, "CITY");
                                string state = DBString(rdr, "STATE");
                                string zip = DBString(rdr, "ZIP");


                                dict.Clear();
                                dict.Add("mdn", mdn);
                                dict.Add("carrier", carrier);

                                dict2.Add("authorized_by", fname + " " + lname);
                                dict2.Add("first_name", fname);
                                dict2.Add("last_name", lname);
                                dict2.Add("street", address);
                                dict2.Add("city", city);
                                dict2.Add("state", state);
                                dict2.Add("zip_code", zip);
                                dict2.Add("account", portacct);
                                dict2.Add("pin", portpasswd);

                                dict.Add("port_details", dict2);
                                jsondata = js.Serialize(dict);
                                rc = SendRequest("subscriptions/" + subscr_id + "/update_port/", "POST", jsondata, ref jsonresult);
                                
                                dict.Clear();
                                dict.Add("pin", pin);
                                jsondata = js.Serialize(dict);
                                rc = ActivateSubscription(subscr_id, jsondata,ref jsonresult);
                                sprintService.Log(jsonresult);

                            }
                            poll = true;
                            UpdateRequest(reqid, reqType,subscr_id, poll, jsondata, jsonresult, null);

                            return true;
                            // Console.ReadLine();
                        }
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        UpdateRequest(reqid, reqType,subscr_id, poll, jsondata,jsonresult, e.Message);
                        return false;
                    }
                    break;

                case ReqType.ChangeESN:

                    if (subscr_id != "" && esn != "")
                    {
                        try
                        {
                            dict.Clear();
                            dict.Add("device_id", esn);
                            dict.Add("pin", pin);
                            jsondata = js.Serialize(dict);
                            rc = ModifySubscriptionDevice(subscr_id, jsondata, ref jsonresult);
                            poll = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            UpdateRequest(reqid, reqType, subscr_id, poll, jsondata, jsonresult, e.Message);
                            return false;
                        }
                    }
                    UpdateRequest(reqid, reqType, subscr_id, poll, jsondata, jsonresult, null);
                    return true;
      

                case ReqType.Poll:

                    subscr_id = DBString(rdr, "SubscrId");

                    if (subscr_id != "")
                    {
                        try
                        {
                            //int rc = SendRequest("subscriptions/" + subscr_id + "/port_info/", "GET", jsondata, ref jsonresult);
                            //Console.WriteLine(jsonresult);
                            poll = true;
                            rc = GetSubscription(subscr_id, ref jsonresult);
                            sprintService.Log(jsonresult);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            UpdateRequest(reqid, reqType, subscr_id, poll, jsondata, jsonresult, e.Message);
                            return false;
                        }
                    }
                    else
                        poll = false;

                    UpdateRequest(reqid, reqType,subscr_id, poll, jsondata, jsonresult, null);
                    return true;

                case ReqType.Suspend:
                case ReqType.Restore:
                case ReqType.Expire:
                
                    subscr_id = DBString(rdr, "SubscrId");
                    pin = DBString(rdr, "pin");
                    if (subscr_id != "")
                    {
                        try
                        {
                            jsonresult = "";
                            dict.Clear();

                            if (reqType == ReqType.Expire)
                            {
                                dict.Add("expiry_type", "cancel");
                                pin = "1234";
                            }
                            dict.Add("pin", pin);
                            jsondata = js.Serialize(dict);
                            string requrl = "subscriptions/" + subscr_id + "/" + reqType.ToString().ToLower() + "/";
                            rc = SendRequest(requrl, "POST", jsondata, ref jsonresult);
                            sprintService.Log(jsonresult);
                        
                            // Removed 110220
                            //if (reqType == ReqType.Restore)
                            //{
                            //    dict.Clear();
                            //    dict.Add("plan_id", planid.ToString());
                            //    jsondata = js.Serialize(dict);
                            //    rc = ModifySubscriptionPlan(subscr_id, jsondata, ref jsonresult);
                            //}

                            poll = true;
                        }

                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            UpdateRequest(reqid, reqType, subscr_id, poll, jsondata, jsonresult, e.Message);
                            return false;
                        }
                    }
                    else
                        poll = false;

                    UpdateRequest(reqid, reqType, subscr_id, poll, jsondata, jsonresult, null);
                    return true;
            }
            return false;
        }

        static int SendRequest(string requrl,string method,string jsondata,ref string jsonresult)
        {
            int rc = -1;
            jsonresult = "";
            lastreq = requrl;

            // ... Use HttpClient.            
            HttpClient client = new HttpClient();

            //dev var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
            var byteArray = Encoding.ASCII.GetBytes(String.Format("{0}:{1}",user,pwd));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                HttpRequestMessage request = new HttpRequestMessage();
                request.RequestUri = new Uri(WINGURL + requrl);
                request.Method = new HttpMethod(method);
                if (jsondata != "")
                    request.Content = new StringContent(jsondata, Encoding.UTF8, "application/json");
                Console.WriteLine(method + ":" + request.RequestUri);

                sprintService.Log(string.Format("ReqUrl={0},JsonData={1}", requrl, StrTrunc(jsondata, 200)));

                HttpResponseMessage response = client.SendAsync(request).Result;
                 HttpContent content = response.Content;
                        
                 rc = (int)response.StatusCode;                        
                 Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                jsonresult = content.ReadAsStringAsync().Result;

                sprintService.Log(string.Format("rc={0} jsonresult={1}", rc.ToString(), StrTrunc(jsonresult, 500)));
               
                if (rc > 205)
                    throw new RequestError(jsonresult);
 
            return rc;
        }

        private static bool UpdateRequest(int reqid,ReqType reqType,string subscr_id, bool Poll,String jsondata,String jsonresult,String errorstr)
        {
            String updatestr = "Update SprintReq set prod=prod ";
            String fieldval = "";
            String mdn = "", msid = "",msl = "",status = "",esn="";

          
            fieldval += String.Format(",reqstatus = case when isnull(reqstatus,'') = '' then '{0}' else reqstatus end " +
                                      ",reqackmsg = case when isnull(reqackmsg,'') = '' then '{1}' else reqackmsg end", lastreq, StrTrunc(jsondata, 200));

            if (errorstr != null)
            {
                fieldval += String.Format(",respstatus='Error',respackmsg='{0}',poll=0,processed=getdate()", StrTrunc(errorstr, 200));
                Poll = false;
            }
            else
            {
                dynamic dobj = js.Deserialize<dynamic>(jsonresult);

               // if (jsonresult.IndexOf("subscription") > -1)
               //     dobj = dobj["subscription"];

                try
                {
                   if (jsonresult.IndexOf("sprint_status") > -1)
                        status = dobj["sprint_status"].ToString();
                   if (jsonresult.IndexOf("mdn") > -1)
                        mdn = dobj["mdn"].ToString();
                   if (jsonresult.IndexOf("msid") > -1)
                        msid = dobj["msid"].ToString();
                   if (jsonresult.IndexOf("msl_code") > -1)
                        msl = dobj["msl_code"].ToString();
                   if (jsonresult.IndexOf("device_id") > -1)
                        esn = dobj["device_id"].ToString();
                }
                catch (Exception e)
                {
                    sprintService.Log(string.Format(e.Message + " Error Parsing Json:", StrTrunc(jsonresult, 1000)));
                }
               
                if (esn != "")
                    fieldval += String.Format(",esn='{0}'", esn);

                Console.WriteLine(String.Format("Status={0},MDN={1},MSID={2},MSL={3}",status,mdn,msid,msl));
                fieldval += String.Format(",mdn=case when isnull(mdn,'') = '' then '{0}' else mdn end,msid='{1}',msl='{2}',respstatus='{3}'", mdn, msid, msl, status);

                fieldval += String.Format(",respackmsg='{0}'", StrTrunc(jsonresult,1000));

                //if (status == "" || status == "new" | status == "active")
                if (status == "new" || status == "active"  || status == "expired" || status == "suspended")
                    Poll = false;
                 
                if (Poll == true)
                    fieldval += String.Format(",poll='True',pollcount=pollcount+1");
                else
                    fieldval += String.Format(",Processed=getdate(),poll='False'");
            }

            if (reqid > 0)  // not testing
            {
                SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.Connection = sqlconn;
                try
                {
                    sqlCommand.CommandText = updatestr += fieldval + String.Format(" where SprintReqid = {0}", reqid);
                    sprintService.Log(sqlCommand.CommandText);
                    sqlCommand.ExecuteNonQuery();
                }

                catch (Exception ex)
                {
                    sprintService.Log("Error updating SprintReq Status - " + ((object)ex.Message).ToString());
                    return false;
                }

                if (status == "active" || status == "expired" || status == "suspended")
                {
                    ProcessResponse(reqid.ToString(), mdn, status);
                }
            }
            return true;
        }

        static bool ProcessResponse(string reqid,string mdn,string status)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = String.Format("exec ppc.dbo.Sprint_Resp {0},'{1}','{2}'", reqid,mdn,status);
                sprintService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();
                return true;

            }
            catch (Exception ex)
            {
                sprintService.Log("Error updating SprintReq Status - " + ((object)ex.Message).ToString());
                return false;
            }
        }

        static void SynchSubscriptions(string jsonresult,SqlCommand sqlcmd)
        {
            int page = 1;
            int rc; 
            do
            {
                dynamic dobj = js.Deserialize<dynamic>(jsonresult);

                var ja = dobj["results"];

                foreach (var result in ja)
                    SynchSub(result,sqlcmd);

                page = page + 1;
                rc = SendRequest("subscriptions/?page=" + page.ToString(), "GET", "", ref jsonresult);
            }
            while (rc == 200);
        }

        static void SynchSub(object result,SqlCommand sqlcmd)
        {
               
                Dictionary<string, object> obj2 = (Dictionary<string, object>)(result);
                sqlcmd.CommandText = String.Format("exec ppc.dbo.SprintSyncMDN '{0}','{1}','{2}','{3}','{4}','{5}'", obj2["mdn"], obj2["msid"], obj2["msl_code"], obj2["id"], obj2["device_id"], obj2["sprint_status"]);
                sqlcmd.ExecuteNonQuery();

                Console.WriteLine(obj2["mdn"] + "= subscrid:" + obj2["id"]);
        }

        static int ModifyCSA(string subscr_id, string jsondata,ref string jsonresult)
        {
            jsonresult = "";
            String requrl  = "subscriptions/" + subscr_id + '/';
            int rc = SendRequest(requrl, "PATCH", jsondata, ref jsonresult);
            return rc;
        }

        static int ModifySubscriptionDevice(string subscr_id,string jsondata, ref string jsonresult)
        {
            jsonresult = "";
            string requrl = "subscriptions/" + subscr_id + "/update_device/";
            int rc = SendRequest(requrl, "POST", jsondata, ref jsonresult);
            return rc;
        }

        static int ModifySubscriptionPlan(string subscr_id, string jsondata,ref string jsonresult)
        {
            jsonresult = "";
            string requrl = "subscriptions/" + subscr_id + "/update_plan/";
            int rc = SendRequest(requrl, "POST", jsondata, ref jsonresult);
            return rc;
        }
        
        static int ActivateSubscription(string subscr_id,string jsondata,ref string jsonresult)
        {
                jsonresult = "";
                string requrl = "subscriptions/" + subscr_id + "/activate/";
                int rc = SendRequest(requrl, "POST", jsondata, ref jsonresult);
                return rc;
        }
            
        static int GetSubscription(string subscr_id,ref string jsonresult)
        {
            jsonresult = "";
            string jsondata = "";
            string requrl = "subscriptions/" + subscr_id + "/";
            int rc = SendRequest(requrl, "GET", jsondata, ref jsonresult);
            return rc;
        }

        static String GetNewSubscription(string fname,string lname)
        {
            string subscr_id = "";
            string jsondata = "";
            string jsonresult = "";

            Dictionary<string, object> dicts = new Dictionary<string, object>();
            dicts.Add("first_name", fname);
            dicts.Add("last_name", lname);
            jsondata = js.Serialize(dicts);

            string requrl = "subscriptions/";
            SendRequest(requrl, "POST", jsondata, ref jsonresult);

            if (jsonresult != null)
            {
                dynamic dobj = js.Deserialize<dynamic>(jsonresult);
                subscr_id = dobj["id"].ToString();
            }
            return subscr_id;
        }

        static String GetCSA(String zipcode, ref String npa, ref String nxx)
        {
            string csa = "";
            string jsondata = "";
            string jsonresult = "";

            npa = "";
            nxx = "";

            string requrl = "sprint/csa/?zip_code=" + zipcode;
            SendRequest(requrl, "GET", jsondata, ref jsonresult);

            if (jsonresult != null)
            {
                dynamic dobj = js.Deserialize<dynamic>(jsonresult);
                csa = dobj["csa"].ToString();
                npa = dobj["npa"].ToString();
                nxx = dobj["nxx"].ToString();

                Console.WriteLine(jsonresult);
            }
            return csa;
        }

        static void GetPlans()
        {
            string jsondata = "";
            string jsonresult = "";
            string requrl = "plans/";
            SendRequest(requrl, "GET", jsondata, ref jsonresult);
                  
            if (jsonresult != null)
                Console.WriteLine(jsonresult);
            return;
        }

        static bool UpdateSprintReq(int reqid,string fldname,string value)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = sqlconn;
            try
            {
                sqlCommand.CommandText = String.Format("Update SprintReq set {0} = '{1}' where SprintReqid = {2}",fldname, value, reqid);
                sprintService.Log(sqlCommand.CommandText);
                sqlCommand.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                sprintService.Log("Error updating SprintReq Status - " + ((object) ex.Message).ToString());
                return false;
            }
            return true;
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
        static Boolean DBBool(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return (Boolean)rdr.GetSqlBoolean(offset);
            return false;
        }

        static Int64 DBInt64(SqlDataReader rdr, String Val)
        {
            int offset = rdr.GetOrdinal(Val);
            if (!rdr.IsDBNull(offset))
                return (Int64)rdr.GetSqlInt64(offset);
            return 0;
        }

        static String StrTrunc(String str,int max)
        {
            str = str.Replace("\"", "");
            if (str.Length > max)
                return str.Substring(0, max - 1);
            return str;
        }
    }
}