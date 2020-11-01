
Module AuthCharge

    Sub Main(ByVal args() As String)

        Dim cc As New CreditCardCharge()

        Dim con As SqlConnection =
        New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim con2 As SqlConnection =
        New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con2.Open()


        Dim rdr As SqlDataReader = Nothing
        Dim cmdstr As String

        cmdstr = "select top 1 case when p1.planid is not null then p1.planid else p2.planid end [planid], "
        cmdstr += "case when p1.planid is not null then p1.monthly_cost else p2.plan_cost end [plancost],c.initial_agent as sales_rep_id "
        cmdstr += "r.renewal_id,r.order_id,r.cell_num,cc_last_four,cc_expiration_date,r.monthly_auto_renew,r.cash_auto_renew,r.intl_auto_renew, o.carrier_name from renewals r join orders o on r.order_id = o.order_id "
        cmdstr += "join customers c on o.order_id = c.customer_id "
        cmdstr += "left outer join Plans p1 on o.monthly_plan_id = p1.planid "
        cmdstr += "left outer join Plans p2 on o.cash_plan_id = p2.planid "
        cmdstr += "where status = 'Pending'"
        cmdstr += "order by renewal_id "

        Dim cmd As New SqlCommand(cmdstr, con)
        Dim writecmdstr As String

        rdr = cmd.ExecuteReader()
        While rdr.Read()

            cc.OrderId = rdr.Item("order_id")
            cc.CellNumber = rdr.Item("cell_num")
            cc.CCNumber = "************" + rdr.Item("cc_last_four")
            cc.CCExpiration = rdr.Item("cc_expiration_date")

            If (rdr.Item("monthly_auto_renew") = True) Then
                cc.MonthlyPlanId = rdr.Item("planid")
                cc.MonthlyAmnt = rdr.Item("plancost")

            ElseIf (rdr.Item("cash_auto_renew") = True) Then
                cc.CashPlanId = rdr.Item("planid")
                cc.CashAmnt = rdr.Item("plancost")

            ElseIf (rdr.Item("intl_auto_renew").ToString() = "1") Then
                cc.IntlAmnt = rdr.Item("r.intl_cost")
            End If

            cc.User = "System"
            cc.Agent = rdr.Item("sales_rep_id")

            Console.WriteLine(String.Format("processing renewal for {0} for ${1}", rdr.Item("cell_num"), rdr.Item("plancost")))

            cc.RunCharge()

            writecmdstr = String.Format("update renewals set trans_id={0},updated=getdate()", cc.TransactionId)

            If cc.hasCharged Then
                Console.WriteLine(String.Format("processed renewal for {0} for ${1} - result = {2}", rdr.Item("cell_num"), rdr.Item("plancost"), cc.AuthMessage()))
                writecmdstr += ",status='Charged',charged=getdate() "
            Else
                Console.WriteLine(String.Format("renewal failed for {0} for ${1} - result = {2}", rdr.Item("cell_num"), rdr.Item("plancost"), cc.AuthMessage()))
                writecmdstr += String.Format(",status='{0}'", Strings.Left(cc.AuthMessage, 50))

            End If
            writecmdstr += String.Format(" where renewal_id = {0}", rdr("renewal_id"))

            If rdr.Item("Carrier_Name").ToString.ToLower = "concord" Then
                writecmdstr += String.Format(" exec ConcordProcessPayment {0}, {1} ", rdr("order_id"), rdr("plan_id"))
            End If

            Dim writecmd As New SqlCommand(writecmdstr, con2)

            Try
                writecmd.ExecuteNonQuery()
            Catch ex As Exception
                Console.WriteLine("Error update renewals -" + ex.Message.ToString())
            End Try

            writecmd.Dispose()

        End While
    End Sub

End Module
