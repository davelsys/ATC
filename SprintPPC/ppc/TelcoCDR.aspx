<%@ Page Title="Telco CDR" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="TelcoCDR.aspx.vb" Inherits="TelcoCDR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">

    <style type="text/css">
        
        .downloadTelCdrDiv {    
            height: 25px;
            margin-top: 10px;
        }
        
        .downloadTelCdrDiv input[type=submit] {
            float: right;
        }
        
    </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div id="mainDiv">
        <div id="telcoCDRGVDiv" style="overflow: scroll; overflow-y: hidden;">
            <asp:GridView ID="telcoCdrGV" runat="server" AutoGenerateColumns="true" Font-Size="11px" Width="100%"
                HeaderStyle-Wrap="false" RowStyle-Wrap="false"
                DataSourceID="telcoCdrGVSqlDataSource" AllowSorting="true" AllowPaging="true" PageSize="20">
            </asp:GridView>
            
            <asp:SqlDataSource ID="telcoCdrGVSqlDataSource" runat="server">
            </asp:SqlDataSource>
        </div>
        
        <div id="downloadTelCdrDiv" class="downloadTelCdrDiv" runat="server">
            <asp:Button ID="downloadTelCdrBtn" runat="server" Text="Download" />
        </div>

    </div>

</asp:Content>