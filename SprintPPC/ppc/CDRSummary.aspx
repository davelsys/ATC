<%@ Page Title="Call Summary" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="CDRSummary.aspx.vb" Inherits="CDRSummary" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <h2>
        Call Data Summary
    </h2>
    
    <div class="SearchField" style="width: 300px">
        Account:
        <asp:DropDownList ID="dropAccounts" runat="server" AutoPostBack="true" Width="200px" >
        </asp:DropDownList>
    </div>

    <br /><br />

    <div style="overflow:auto; width: 930px; height: 100%;">
       
         <asp:GridView ID="gvSummary" runat="server" AutoGenerateColumns="true"  ShowHeader="true"
                             DataSourceID="SqlDataSource1" Font-Size="11px"  Width="98.3%" EnableViewState="false" 
                             RowStyle-CssClass="GridOut" AlternatingRowStyle-CssClass="GridOutAlt" >
         </asp:GridView>
                  
        <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="" CancelSelectOnNullParameter="false">
        </asp:SqlDataSource>

    </div>
</asp:Content>

