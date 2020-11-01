// Type: DB
// Assembly: VerizonLTC, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 420BB13D-AA0E-4037-836F-1D0288F21D13
// Assembly location: C:\VerLTCNV\VerizonLTC051815.exe

using System;
using System.Data.SqlClient;
using System.Net;

internal class DB
{
    public SqlConnection sqlconn = (SqlConnection)null;

    public bool OpenDB(string Database, string UserArg, string PwdArg)
    {
        this.sqlconn = new SqlConnection();
        string str1 = "(local)";
        string str2 = UserArg != "" ? UserArg : "sa";
        string str3 = PwdArg != "" ? PwdArg : "davel";
        string hostName = Dns.GetHostName();
        if (hostName == "dvlvostro")
            str1 = "NEWHP2";
        string str4 = "Data Source=" + str1 + ";User ID=" + str2 + ";Password=" + str3 + ";MultipleActiveResultSets=True;Initial Catalog=" + Database;
        Console.WriteLine("Computer name :" + hostName + " DB :" + Database + "(Conn=" + str4 + ")");
        try
        {
            this.sqlconn.ConnectionString = str4;
            this.sqlconn.Open();
            Console.WriteLine("Database Opened");
        }
        catch (Exception ex)
        {
            if (this.sqlconn != null)
                this.sqlconn.Dispose();
            Console.WriteLine("A error occurred while trying to connect to the server." + Environment.NewLine + Environment.NewLine + ex.Message);
            return false;
        }
        return true;
    }

    public void CloseDB()
    {
        if (this.sqlconn == null)
            return;
        this.sqlconn.Close();
        this.sqlconn.Dispose();
    }
}
