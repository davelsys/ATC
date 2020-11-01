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
    class Program
    {

        static String user = ConfigurationManager.AppSettings["Sprint_User"]; //"TSP_AGENTS_LITECALL";
        static string pwd = ConfigurationManager.AppSettings["Sprint_PWD"]; //"buBeh3st";

        static String WINGURL = "https://api-dev.wingalpha.com/api/";

        static Svc sprintService = new Svc();
        enum ReqType { NewService, ChangeESN, Port,Poll};/*OrderInquiry, PortValidation, ValidationInquiry };*/

        private static bool prodmode;
 

        private static SqlConnection sqlconn;

        static void Main()//)(string[] args)
        {
            sprintService.OpenLog("C:\\Sprint\\SprintLog.txt");

            DB db = new DB();
            db.OpenDB("Sprint", "sa3", "davel");

            sqlconn = db.sqlconn;

            SqlCommand sqlCommand = new SqlCommand(string.Format("SprintGetOpenReq", new object[0]), Program.sqlconn);
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
                            sprintService.Log("\n=======================================");
                            for (int index = 0; index < sqlDr.FieldCount; ++index)
                            {
                                args[index] = sqlDr[index].ToString();
                                sprintService.Log(sqlDr.GetName(index) + "=" + sqlDr[index] + " ");

                            }
                            sprintService.Log("\n===");

                            int reqid = (int)sqlDr["SprintreqId"];
                            bool pollreq = DBBool(sqlDr, "Poll");


                            ReqType reqtype = (ReqType)ReqType.Parse(typeof(ReqType), DBString(sqlDr, "ReqType"), true);
                            DateTime now = DateTime.Now;

                            bool reqrc;
                            if (pollreq)
                                reqrc = SprintRequest(reqid,ReqType.Poll , sqlDr, now);
                            else
                                reqrc = SprintRequest(reqid, reqtype, sqlDr, now);

                            sprintService.Log(string.Format("Reqid = {0}, Req={1}, Result = {2}", reqid.ToString(), reqtype.ToString(), (object)reqrc.ToString()));
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
                sprintService.Log("****Main ERROR MSG: " + ex.Message);
                sprintService.Log("****Main ERROR Trace: " + ex.StackTrace);

            }
            db.CloseDB();
        }


        static bool SprintRequest(int reqid, ReqType reqType, SqlDataReader rdr, DateTime dt)
        {
            int planid = DBInt32(rdr, "PlanId");// 10605; 
            int payid = planid;
            int cycle = 1;

            int orderid = DBInt32(rdr, "OrderId");

            string esn = DBString(rdr, "ESN");
            string mdn = DBString(rdr, "MDN");
            string pin = DBString(rdr, "Pin");
            string subscr_id = DBString(rdr, "SubscrId");
            string zip = DBString(rdr, "NPAZip");
       



            /*
            string account = "1166";
            string refnum = "LTC_16272813488";
            string icc = "";
            string callbackurl = "";
            //string refnum = REF(DateTime.Now);
            //string respmsg = "";
              */

            bool hasError = false;
            string errorMsg = "";
            string jsondata = "";
            string jsonresult = "";
            bool poll = false;
          
            string CSA;
            string npa = "";
            string nxx = "";
            

            Dictionary<string, string> dict = new Dictionary<string, string>();
            var js = new JavaScriptSerializer();

            switch (reqType)
            {         
                   case ReqType.NewService:
                    if (mdn == "")
                    {
                        try
                        {
                            subscr_id = "309";  //GetNewSubscription();

                            CSA = GetCSA(zip, ref npa, ref nxx);

                            dict.Clear();
                            dict.Add("csa", CSA);
                          
                            jsondata = js.Serialize(dict);
                            ModifyCSA(subscr_id, jsondata);


                            dict.Add("plan_id", planid.ToString());
                            jsondata = js.Serialize(dict);
                            jsonresult = ModifySubscriptionPlan(subscr_id, jsondata);

                            Console.WriteLine(jsonresult);

                            dict.Clear();
                            dict.Add("device_id", esn);

                            jsondata = js.Serialize(dict);
                            jsonresult = ModifySubscriptionDevice(subscr_id, jsondata);
                            Console.WriteLine(jsonresult);

                            //GetPlans();
                            //var subscr = GetNewtSubscription();
                            //  var csa = GetCSA("11230");
                            //Task t = new Task(HTTP_GET);
                            //t.Start(Action(GetCSA,"11230"));

                            dict.Clear();
                            dict.Add("pin", pin);
                            dict.Add("npa", npa);
                            jsondata = js.Serialize(dict);
                            jsonresult = ActivateSubscription(subscr_id, jsondata);
                            Console.WriteLine(jsonresult);

                            UpdateRequest(reqid, subscr_id, rdr, poll,jsonresult, null);

                            Console.ReadLine();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            UpdateRequest(reqid, subscr_id, rdr, poll, "", e.Message);
                            return false;
                        }
                        return true;
                    }
                    break;

                case ReqType.Port:
                    try
                    {
                        if (mdn != "")
                        {
                            int rc = SendRequest("ValidatePort", "sprint/validate_port/?mdn=" + mdn, jsondata,ref jsonresult);
                           
                            if (rc == 200)
                            {
                                dynamic dobj = js.Deserialize<dynamic>(jsonresult);
                                string carrier = dobj["carrier"];


                                if (subscr_id == "")
                                   subscr_id = "310";// GetNewSubscription();
                                
                                dict.Clear();
                                dict.Add("plan_id", planid.ToString());
                           
                                jsondata = js.Serialize(dict);
                                jsonresult = ModifySubscriptionPlan(subscr_id, jsondata);

                                Console.WriteLine(jsonresult);

                                dict.Clear();
                                dict.Add("device_id", esn);
                                jsondata = js.Serialize(dict);
                                jsonresult = ModifySubscriptionDevice(subscr_id, jsondata);
                                Console.WriteLine(jsonresult);

                                //GetPlans();
                                //var subscr = GetNewtSubscription();
                                //  var csa = GetCSA("11230");
                                //Task t = new Task(HTTP_GET);
                                //t.Start(Action(GetCSA,"11230"));

                                string fname = DBString(rdr, "FNAME");
                                string lname = DBString(rdr, "LNAME");
                                string address = DBString(rdr, "ADDRESS");
                                string city = DBString(rdr, "CITY");
                                string state = DBString(rdr, "STATE");
                                

                                dict.Clear();
                                dict.Add("Authorized By", fname + " " + lname);
                                dict.Add("First Name", fname);
                                dict.Add("Last Name", lname);
                                dict.Add("Street",address);
                                dict.Add("City", city);
                                dict.Add("State", state);
                                dict.Add("Zip Code", zip);
                                dict.Add("Account", "");
                                dict.Add("Pin", pin);
                                String portdet = js.Serialize(dict);

                                dict.Clear();
                                dict.Add("Port Details",portdet);
                                dict.Add("mdn", mdn);
                                dict.Add("carrier", carrier);

                                jsondata = js.Serialize(dict);
                                 jsonresult = ActivateSubscription(subscr_id, jsondata);
                                Console.WriteLine(jsonresult);
                                jsondata = js.Serialize(dict);
                                rc = SendRequest("Port", "subscriptions/" + subscr_id + "/update_port/", jsondata,ref jsonresult);
                                Console.WriteLine(jsonresult);

                            }
                            poll = true;
                            UpdateRequest(reqid, subscr_id, rdr, poll,jsonresult, null);
                            Console.ReadLine();
                        }
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        UpdateRequest(reqid, subscr_id, rdr, poll, null, e.Message);
                        return false;
                    }
                    break;
                   
                case ReqType.Poll:

                    subscr_id = DBString(rdr, "SubscrId");

                    if (subscr_id != "")
                    {
                        try
                        {
                            int rc = SendRequest("Poll", "subscriptions/" + subscr_id + "/port_info/", jsondata, ref jsonresult);
                            Console.WriteLine(jsonresult);


                            UpdateRequest(reqid, subscr_id, rdr, poll, jsonresult, null);

                            Console.ReadLine();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            UpdateRequest(reqid, subscr_id, rdr, poll, null, e.Message);
                            return false;
                        }
                    }
                    return true;
 
            }
            return false;
        }

        static int SendRequest(string requrl,string method,string jsondata,ref string jsonresult)
        {
            int rc = -1;
            jsonresult = "";
            try
            {
                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                HttpRequestMessage request = new HttpRequestMessage();
                request.RequestUri = new Uri(WINGURL + requrl);
                request.Method = new HttpMethod(method);
                request.Content = new StringContent(jsondata, Encoding.UTF8, "application/json");
                Console.WriteLine(method + ":" + request.RequestUri);

                HttpResponseMessage response = client.SendAsync(request).Result;
                HttpContent content = response.Content;

                rc = (int)response.StatusCode;                        
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                jsonresult = content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return rc;
        }

        private static bool UpdateRequest(int reqid, string subscr_id,SqlDataReader rdr, bool Poll,String msg = "",String error = "")
        {
            String updatestr = "Update SprintReq set prod=prod ";
            String fieldval = "";
            String mdn = "", msid = "",jsonresult="",status = "",subscrstr= "",pollstr = "";

            if (subscr_id != "")
            {
                jsonresult = GetSubscription(subscr_id);
                JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                dynamic dobj = jsonSerializer.Deserialize<dynamic>(jsonresult);
                mdn = dobj["mdn"].ToString();
                msid = dobj["msid"].ToString();
                status = dobj["sprint_status"].ToString();
                subscrstr = dobj["subscrid"];
                pollstr = dobj["poll"].ToString();
                Console.WriteLine(jsonresult);
            }
            fieldval = String.Format(",Processed=getdate(),mdn='{0}',msid='{1}',subscrid='{2}',poll={3},respackmsg='{4}'", mdn, msid,subscr_id.ToString(),Poll.ToString(),status);

            if (fieldval.Length == 0)
                return false;

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
            return true;
        }

        static String ModifyCSA(string subscr_id, string jsondata)
        {
            string jsonresult = "";
            try
            {
                String TARGETURL = WINGURL + "subscriptions/";

                Console.WriteLine("PATCH: + " + TARGETURL);


                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                HttpRequestMessage request = new HttpRequestMessage();
                request.Method = new HttpMethod("PATCH");
                request.RequestUri = new Uri(TARGETURL + subscr_id + '/');
                request.Content = new StringContent(jsondata, Encoding.UTF8, "application/json");


                HttpResponseMessage response = client.SendAsync(request).Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                jsonresult = content.ReadAsStringAsync().Result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return jsonresult;
        }

        static String ModifySubscriptionDevice(string subscr_id, string jsondata)
        {
            string jsonresult = "";
            try
            {
                String TARGETURL = WINGURL + "subscriptions/";

                Console.WriteLine("MODIFY: + " + TARGETURL);

                // ... Use HttpClient.            


                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                HttpRequestMessage request = new HttpRequestMessage
                {
                    Method = new HttpMethod("POST"),
                    RequestUri = new Uri(TARGETURL + subscr_id + "/update_device/"),
                    Content = new StringContent(jsondata, Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response = client.SendAsync(request).Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                jsonresult = content.ReadAsStringAsync().Result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            return jsonresult;
        }

        static String ModifySubscriptionPlan(string subscr_id, string jsondata)
        {
            string jsonresult = "";
            try
            {
                String TARGETURL = WINGURL + "subscriptions/";

                Console.WriteLine("MODIFY: + " + TARGETURL);

                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                HttpRequestMessage request = new HttpRequestMessage
                {
                    Method = new HttpMethod("POST"),
                    RequestUri = new Uri(TARGETURL + subscr_id + "/update_plan/"),
                    Content = new StringContent(jsondata, Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response = client.SendAsync(request).Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                jsonresult = content.ReadAsStringAsync().Result;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            return jsonresult;
        }

static String ActivateSubscription(string subscr_id,string jsondata)
{
    string jsonresult = "";
    try
    {
        String TARGETURL = WINGURL + "subscriptions/";

        Console.WriteLine("MODIFY: + " + TARGETURL);

        // ... Use HttpClient.            
        HttpClient client = new HttpClient();

        var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        HttpRequestMessage request = new HttpRequestMessage
        {
            Method = new HttpMethod("POST"),
            RequestUri = new Uri(TARGETURL + subscr_id + "/activate/"),
            Content = new StringContent(jsondata, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = client.SendAsync(request).Result;
        HttpContent content = response.Content;

        // ... Check Status Code                                
        Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

        // ... Read the string.
        jsonresult = content.ReadAsStringAsync().Result;

    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
    return jsonresult;
}

static String GetSubscription(string subscr_id)
        {
            string jsonresult = "";
            try
            {
                String TARGETURL = WINGURL + "subscriptions/";

                Console.WriteLine("POST: + " + TARGETURL);


                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


                HttpResponseMessage response = client.GetAsync(TARGETURL + subscr_id + '/').Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                jsonresult = content.ReadAsStringAsync().Result;


                if (jsonresult != null)
                {
                    JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                    dynamic dobj = jsonSerializer.Deserialize<dynamic>(jsonresult);
                    subscr_id = dobj["id"].ToString();
                    Console.WriteLine(jsonresult);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            return jsonresult;
        }

        static String GetNewSubscription()
        {
            string subscr_id = "";
            try
            {
                String TARGETURL = WINGURL + "subscriptions/";

                Console.WriteLine("POST: + " + TARGETURL);


                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


                HttpResponseMessage response = client.PostAsync(TARGETURL, null).Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                string jsonresult = content.ReadAsStringAsync().Result;


                if (jsonresult != null)
                {
                    JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                    dynamic dobj = jsonSerializer.Deserialize<dynamic>(jsonresult);
                    subscr_id = dobj["id"].ToString();
                    Console.WriteLine(jsonresult);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            return subscr_id;
        }

        static String GetCSA(String zipcode,ref String npa,ref String nxx)
        {
            string csa = "";
            try
            {
                String TARGETURL = WINGURL + "sprint/csa/?zip_code=" + zipcode;

                Console.WriteLine("GET: + " + TARGETURL);

                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


                HttpResponseMessage response = client.GetAsync(TARGETURL).Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                string jsonresult = content.ReadAsStringAsync().Result;

                npa = "";
                nxx = "";

                if (jsonresult != null)
                {
                    JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                    dynamic dobj = jsonSerializer.Deserialize<dynamic>(jsonresult);
                    csa = dobj["csa"].ToString();
                    npa = dobj["npa"].ToString();
                    nxx = dobj["nxx"].ToString();

                    Console.WriteLine(jsonresult);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            return csa;
        }


        static void GetPlans()
        {
            try
            {
                String TARGETURL = WINGURL + "plans/";

                Console.WriteLine("GET: + " + TARGETURL);

                // ... Use HttpClient.            
                HttpClient client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("F2DVZJHbQy0BrOXm:yEgGA4oBDwhr0z0OpTkq83He");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


                HttpResponseMessage response = client.GetAsync(TARGETURL).Result;
                HttpContent content = response.Content;

                // ... Check Status Code                                
                Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

                // ... Read the string.
                string jsonresult = content.ReadAsStringAsync().Result;

                if (jsonresult != null)
                {
                    JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                    dynamic dobj = jsonSerializer.Deserialize<dynamic>(jsonresult);

                    Console.WriteLine(jsonresult);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            return;
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

    }
}




//enum ReqType { NewService, OrderInquiry, PortValidation, ValidationInquiry };
/*
 
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
                telcoPort.OldCarrierAccountNumber = DBString(rdr, "PORTACCT"); ;
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
*/
