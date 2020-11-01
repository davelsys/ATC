<%@ Page Title="" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="VerIVR.aspx.vb" Inherits="VerIVR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">
 
  <div id="searchPinsPnl" class="searchDivPins" style="margin-bottom: 0; padding: 0;">  

         <div id="searchVerDiv" style="width: 50%;">
          <fieldset>
            <legend>Search</legend><span>
                <label>
                      Cell #:</label>
                <span class="globalSearchFld">
                    <asp:TextBox ID="phoneNumberSearchFld" runat="server" ClientIDMode="Static" Width="151px"></asp:TextBox>
                    <img alt="" src="images/delete.png" title="Clear global search." /></span>
                    <span>
                    <asp:Button ID="searchBtn" runat="server" Text="Go" />
                </span>
                </span>
          </fieldset>
       </div>
     </div>


    <div style="overflow:auto; width: 930px; height: 100%;">
      
        <asp:GridView ID="ivrvalGridview" runat="server" Width="98.3%" AutoGenerateColumns="false"
                        DataSourceID="ivrvalGridviewDataSource"
                        Font-Size="11px" ViewStateMode="Enabled"  ClientIDMode="static"                        
                        AllowPaging="true" PageSize="25" AllowSorting="True" >
            <Columns>
                <asp:BoundField DataField="CallerID" HeaderText="CallerID" SortExpression="CallerID" />
                <asp:BoundField DataField="CellNum" HeaderText="CellNum" SortExpression="CellNum" />
                <asp:BoundField DataField="CallTime" HeaderText="CallTime" SortExpression="CallTime"/>
                <asp:BoundField DataField="Activity" HeaderText="Activity" SortExpression="Activity" />
                <asp:BoundField DataField="CreditCard" HeaderText="CreditCard" SortExpression="CreditCard" />
                <asp:BoundField DataField="NewPlan" HeaderText="NewPlan" SortExpression="NewPlan" />
                <asp:BoundField DataField="CurrentPlan" HeaderText="CurrentPlan" SortExpression="CurrentPlan" />
                <asp:BoundField DataField="RenewAttempt" HeaderText="RenewAttempt" SortExpression="RenewAttempt"/>
                <asp:BoundField DataField="RenewResult" HeaderText="RenewResult" SortExpression="RenewResult"/>
            </Columns>
        </asp:GridView>

        <asp:SqlDataSource ID="ivrvalGridviewDataSource" runat="server"
            ConnectionString="<%$ ConnectionStrings:ivrConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ivrConnectionString.ProviderName %>" > 
             <SelectParameters>
                <asp:ControlParameter ControlID="phoneNumberSearchFld" Name="PhoneNumber" DefaultValue="%"
                    Type="String" />
            </SelectParameters>
        </asp:SqlDataSource>

        <input type="hidden" id="selectedRowIndex" value="-1" />

    </div>
</asp:Content>

