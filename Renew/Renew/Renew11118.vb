Imports AuthCharge

Module Renew

    Sub Main(ByVal args() As String)

        Dim consp As SqlConnection =
        New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        consp.Open()

        Dim con As SqlConnection =
        New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con.Open()

        Dim con2 As SqlConnection =
        New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
        con2.Open()


        Dim renew_sp As New SqlCommand("exec SetRenewals", consp)
        Try
            renew_sp.CommandTimeout = 64000
            Console.WriteLine(String.Format("SP SetRenewals .. (Timeout = {0})", renew_sp.CommandTimeout))
            renew_sp.ExecuteNonQuery()
        Catch ex As Exception
            Console.WriteLine("Error running SetRenewals SP -" + ex.Message.ToString())
        End Try

        consp.Dispose()

        Dim rdr As SqlDataReader = Nothing
        Dim cmdstr As String
        Dim renewal_id As Int32 = 0
        Dim renew_charge_type As Int32 = 0
        Dim carrier As String = ""

        'SG 12/30/16 Changed the join to join on customer_id rather than order_id

        cmdstr = "select case when p1.planid is not null then p1.planid else p2.planid end [planid], "
        cmdstr += "case when p1.planid is not null then isnull(p1.autopay_cost,p1.monthly_cost) else p2.plan_cost end [plancost], " 'SG 01/05/17 get autopay pricing
        cmdstr += "o.carrier_name, "
        cmdstr += "r.renewal_id,r.order_id,r.cell_num,r.renew_charge_type,cc_last_four,cc_expiration_date,r.monthly_auto_renew,r.cash_auto_renew,r.intl_auto_renew, r.agent_id from renewals r "
        cmdstr += "join customers c on r.Customer_id = c.customer_id "
        cmdstr += "left outer join Plans p1 on r.monthly_plan_id = p1.planid "
        cmdstr += "left outer join Plans p2 on r.cash_plan_id = p2.planid "
        cmdstr += "join orders o on o.cell_num = r.cell_num "
        cmdstr += "where status = 'Pending' "
        cmdstr += "order by renewal_id "

        Console.WriteLine(cmdstr)


        Dim cmd As New SqlCommand(cmdstr, con)

        rdr = cmd.ExecuteReader()
        While rdr.Read()

            Dim cc As New CreditCardCharge()
            Dim agt As New AgentCharge()

            cc.OrderId = rdr.Item("order_id")
            cc.CellNumber = rdr.Item("cell_num")
            carrier = rdr.Item("carrier_name")

            If Not IsDBNull(rdr.Item("cc_last_four")) Then
                cc.CCNumber = "************" + rdr.Item("cc_last_four")
                cc.CCExpiration = rdr.Item("cc_expiration_date")
            End If

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
            renewal_id = rdr.Item("renewal_id")
            renew_charge_type = rdr.Item("renew_charge_type")

            If Not IsDBNull(rdr.Item("agent_id")) Then
                cc.Agent = rdr.Item("agent_id")
            End If

            Console.WriteLine(String.Format("processing  renewal for {0}   Method = {1} Agent = {2} CCnum = {3}",
                                            cc.CellNumber, renew_charge_type, cc.Agent, cc.CCNumber))

            If renew_charge_type = 1 Or renew_charge_type = 3 Then

                Console.WriteLine(String.Format("processing cc renewal for {0} for ${1}", cc.CellNumber, cc.varTotal))

                cc.RunCharge()

                ProcessResult(1, cc.hasCharged, cc.TransactionId, cc.CellNumber, cc.varTotal, cc.AuthMessage, renewal_id, con2)

            End If

            If renew_charge_type = 2 Or (renew_charge_type = 3 And cc.hasCharged = 0 And cc.Agent <> "") Then
                agt.Agent = cc.Agent
                agt.OrderId = cc.OrderId
                agt.CellNumber = cc.CellNumber
                agt.MonthlyPlanId = cc.MonthlyPlanId
                agt.MonthlyAmnt = cc.MonthlyAmnt
                agt.CashPlanId = cc.CashPlanId
                agt.CashAmnt = cc.CashAmnt
                agt.IntlAmnt = cc.IntlAmnt
                agt.User = "System"

                Console.WriteLine(String.Format("processing agent renewal for {0} for ${1}", agt.CellNumber, agt.varTotal))
                agt.RunAgentAccountCharge()
                ProcessResult(0, agt.hasCharged, agt.TransactionId, agt.CellNumber, agt.varTotal, agt.AuthMessage, renewal_id, con2)

            ElseIf renew_charge_type = 3 And cc.Agent = "" Then
                ProcessResult(2, cc.hasCharged, cc.TransactionId, cc.CellNumber, cc.varTotal, cc.AuthMessage, renewal_id, con2)
            End If



            'Process All Talk Carrier Pins
            If carrier.ToLower() = "all talk" And (cc.hasCharged Or agt.hasCharged) Then

                Dim conat As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                conat.Open()

                Dim atc_renew_sp As New SqlCommand("exec ATC_Assign_Pin @cellnum,@planname,@transid", conat)
                Try
                    If agt.hasCharged Then
                        atc_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = agt.CellNumber
                        atc_renew_sp.Parameters.Add("@planname", SqlDbType.Int).Value = agt.MonthlyPlanId
                        atc_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = agt.TransactionId
                    ElseIf cc.hasCharged Then
                        atc_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = cc.CellNumber
                        atc_renew_sp.Parameters.Add("@planname", SqlDbType.Int).Value = cc.MonthlyPlanId
                        atc_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = cc.TransactionId
                    End If

                    atc_renew_sp.CommandTimeout = 64000
                    Console.WriteLine(String.Format("SP ATC_Assign_Pin .. (Timeout = {0})", atc_renew_sp.CommandTimeout))
                    atc_renew_sp.ExecuteNonQuery()
                Catch ex As Exception
                    Console.WriteLine("Error running ATC_Assign_Pin -" + ex.Message.ToString())
                End Try

                conat.Dispose()
            End If
            'End Process All Talk Carrier Pins

            'Process Verizon
            If carrier.ToLower() = "verizon" And (cc.hasCharged Or agt.hasCharged) Then

                Dim conat As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                conat.Open()

                Dim ver_renew_sp As New SqlCommand("exec Ver_Assign_Payment @cellnum,@planid,@transid", conat)
                Try
                    If agt.hasCharged Then
                        ver_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = agt.CellNumber
                        ver_renew_sp.Parameters.Add("@planid", SqlDbType.Int).Value = agt.MonthlyPlanId
                        ver_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = agt.TransactionId
                    ElseIf cc.hasCharged Then
                        ver_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = cc.CellNumber
                        ver_renew_sp.Parameters.Add("@planid", SqlDbType.Int).Value = cc.MonthlyPlanId
                        ver_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = cc.TransactionId
                    End If

                    ver_renew_sp.CommandTimeout = 64000
                    Console.WriteLine(String.Format("SP Ver_Assign_Payment .. (Timeout = {0})", ver_renew_sp.CommandTimeout))
                    ver_renew_sp.ExecuteNonQuery()
                Catch ex As Exception
                    Console.WriteLine("Error running Ver_Assign_Payment -" + ex.Message.ToString())
                End Try

                conat.Dispose()
            End If
            'Process Concord
            If carrier.ToLower() = "concord" Then
                Dim conat As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                conat.Open()

                Dim concord_renew As New SqlCommand("exec ConcordNewPayment @orderid, @cellnum, @planid, @transid", conat)
                Try
                    If agt.hasCharged Then
                        concord_renew.Parameters.Add("@orderid", SqlDbType.Int).Value = agt.OrderId
                        concord_renew.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = agt.CellNumber
                        concord_renew.Parameters.Add("@planid", SqlDbType.Int).Value = agt.MonthlyPlanId
                        concord_renew.Parameters.Add("@transid", SqlDbType.Int).Value = agt.TransactionId

                    ElseIf cc.hasCharged Then
                        concord_renew.Parameters.Add("@orderid", SqlDbType.Int).Value = cc.OrderId
                        concord_renew.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = cc.CellNumber
                        concord_renew.Parameters.Add("@planid", SqlDbType.Int).Value = cc.MonthlyPlanId
                        concord_renew.Parameters.Add("@transid", SqlDbType.Int).Value = cc.TransactionId

                    End If

                    concord_renew.CommandTimeout = 64000
                    Console.WriteLine(String.Format("SP ConcordNewPayment .. (Timeout = {0})", concord_renew.CommandTimeout))
                    concord_renew.ExecuteNonQuery()
                Catch ex As Exception
                    Console.WriteLine("Error running ConcordNewPayment -" + ex.Message.ToString())
                End Try

                conat.Dispose()
            End If
            'Process Telco
            If carrier.ToLower() = "telco" And (cc.hasCharged Or agt.hasCharged) Then

                Dim conat As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                conat.Open()

                Dim telco_renew_sp As New SqlCommand("exec telco_AssignPayment @cellnum,@planid,@transid", conat)
                Try
                    If agt.hasCharged Then
                        telco_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = agt.CellNumber
                        telco_renew_sp.Parameters.Add("@planid", SqlDbType.Int).Value = agt.MonthlyPlanId
                        telco_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = agt.TransactionId
                    ElseIf cc.hasCharged Then
                        telco_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = cc.CellNumber
                        telco_renew_sp.Parameters.Add("@planid", SqlDbType.Int).Value = cc.MonthlyPlanId
                        telco_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = cc.TransactionId
                    End If

                    telco_renew_sp.CommandTimeout = 64000
                    Console.WriteLine(String.Format("SP Telco_AssignPayment .. (Timeout = {0})", telco_renew_sp.CommandTimeout))
                    telco_renew_sp.ExecuteNonQuery()
                Catch ex As Exception
                    Console.WriteLine("Error running Telco_AssignPayment -" + ex.Message.ToString())
                End Try

                conat.Dispose()
            End If

            'Process vzpp
            If carrier.ToLower() = "vzpp" And (cc.hasCharged Or agt.hasCharged) Then

                Dim conat As SqlConnection = New SqlConnection(ConfigurationManager.ConnectionStrings("ppcConnectionString").ConnectionString)
                conat.Open()

                Dim vzpp_renew_sp As New SqlCommand("exec VZPP_Pay @cellnum,@transid,@planid", conat)
                Try
                    If agt.hasCharged Then
                        vzpp_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = agt.CellNumber
                        vzpp_renew_sp.Parameters.Add("@planid", SqlDbType.Int).Value = agt.MonthlyPlanId
                        vzpp_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = agt.TransactionId
                    ElseIf cc.hasCharged Then
                        vzpp_renew_sp.Parameters.Add("@cellnum", SqlDbType.VarChar).Value = cc.CellNumber
                        vzpp_renew_sp.Parameters.Add("@planid", SqlDbType.Int).Value = cc.MonthlyPlanId
                        vzpp_renew_sp.Parameters.Add("@transid", SqlDbType.Int).Value = cc.TransactionId
                    End If

                    vzpp_renew_sp.CommandTimeout = 64000
                    Console.WriteLine(String.Format("SP VZPP_Pay .. (Timeout = {0})", vzpp_renew_sp.CommandTimeout))
                    vzpp_renew_sp.ExecuteNonQuery()
                Catch ex As Exception
                    Console.WriteLine("Error running VZPP_Pay -" + ex.Message.ToString())
                End Try

                conat.Dispose()
            End If
            'End Process All Talk Carrier Pins
        End While
    End Sub

    Sub ProcessResult(ByVal cctype As Int16, ByVal hasCharged As Boolean, ByVal TransactionId As Int32, ByVal CellNumber As String, ByVal varTotal As Decimal, ByVal AuthMessage As String,
                      ByVal renewal_id As Int32, ByRef con2 As SqlConnection)

        Dim writecmdstr As String
        Dim commcmdstr As String = Nothing
        writecmdstr = String.Format("update renewals set trans_id={0},updated=getdate(),cc_pay={1}", TransactionId, cctype)

        If hasCharged Then
            Console.WriteLine(String.Format("processed renewal for {0} for ${1} - result = {2}", CellNumber, varTotal, AuthMessage))
            writecmdstr += ",status='Charged',charged=getdate() "

            commcmdstr = String.Format("InsertTransCommission {0}", TransactionId)
        Else
            If cctype <> 2 Then
                Console.WriteLine(String.Format("renewal failed for {0} for ${1} - result = {2}", CellNumber, varTotal, AuthMessage))
                writecmdstr += String.Format(",status='{0}'", Strings.Left(AuthMessage, 50))
            Else
                Console.WriteLine(String.Format("renewal failed for {0} for ${1} - result = {2}", CellNumber, varTotal, AuthMessage))
                writecmdstr += String.Format(",status='Error'")
            End If
        End If

        writecmdstr += String.Format(" where renewal_id = {0}", renewal_id)

        Dim writecmd As New SqlCommand(writecmdstr, con2)

        Try
            writecmd.ExecuteNonQuery()

            If Not IsNothing(commcmdstr) Then
                writecmd.CommandText = commcmdstr
                writecmd.ExecuteNonQuery()
            End If

        Catch ex As Exception
            Console.WriteLine("Error update renewals -" + ex.Message.ToString())
        End Try

        writecmd.Dispose()
    End Sub

End Module
