<%@ Page Title="PPC CDR" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="SearchCDR.aspx.vb" Inherits="SearchCDR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">
    <h2>
        Search Call Data
    </h2>
    
    <div class="SearchField" style="width: 300px">
        Account:
        <asp:DropDownList ID="dropAccounts" runat="server" AutoPostBack="true" Width="200px" >
        </asp:DropDownList>
    </div>

     <div class="SearchField" style="width: 400px">
        Phone Account:
        <asp:DropDownList ID="dropPhoneAccounts" runat="server" AutoPostBack="false" Width="200px">
        </asp:DropDownList>
    </div>
     
     <div class="SearchField"  style="width: 200px">
        <asp:Button ID="btnSearch" runat="server" Text="Search" />
     </div>

     <br /><br />

    <asp:SqlDataSource ID="SqlDataSource1" runat="server" 
        ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>" 
        
        SelectCommand="SELECT * FROM [PPCData].[dbo].[CDR] WHERE ( @AccountName is null or [AccountName] = @AccountName) and  ( isnull(@PhoneAccountId,-1) = -1 or [PhoneAccountId] = @PhoneAccountId) order by calldate desc">
        <SelectParameters>
            <asp:ControlParameter ControlID="dropAccounts" Name="AccountName" 
                    PropertyName="SelectedValue" Type="String" />

            <asp:ControlParameter ControlID="dropPhoneAccounts" Name="PhoneAccountId" 
                PropertyName="SelectedValue" Type="String" />
        </SelectParameters>
    </asp:SqlDataSource>

    <asp:GridView ID="GridView1" runat="server" AllowPaging="True"  PageSize="50"
        AllowSorting="True" AutoGenerateColumns="False" Font-Size="11px" Width="100%"
        DataKeyNames="PhoneAccountId,PhoneAccount,PhoneNumber,Duration,CallDate,PreBalance,PostBalance,Description" 
        DataSourceID="SqlDataSource1" >
        <Columns>
            <asp:BoundField DataField="ID" HeaderText="ID" InsertVisible="False" 
                ReadOnly="True" SortExpression="ID"  />
            <asp:BoundField DataField="PhoneAccountId" HeaderText="PhoneAccountId" 
                ReadOnly="True" SortExpression="PhoneAccountId" />
            <asp:BoundField DataField="PhoneAccount" HeaderText="PhoneAccount" 
                ReadOnly="True" SortExpression="PhoneAccount" />
            <asp:BoundField DataField="PhoneNumber" HeaderText="PhoneNumber" 
                ReadOnly="True" SortExpression="PhoneNumber" />
            <asp:BoundField DataField="Duration" HeaderText="Duration" ReadOnly="True" 
                SortExpression="Duration" />
            <asp:BoundField DataField="CallDate" HeaderText="CallDate" ReadOnly="True" 
                SortExpression="CallDate" />
            <asp:BoundField DataField="PreBalance" HeaderText="PreBalance" ReadOnly="True" 
                SortExpression="PreBalance" />
            <asp:BoundField DataField="PostBalance" HeaderText="PostBalance" 
                ReadOnly="True" SortExpression="PostBalance" />
            <asp:BoundField DataField="Description" HeaderText="Description" 
                ReadOnly="True" SortExpression="Description" />
     
        </Columns>
       
       
    </asp:GridView>


</asp:Content>

