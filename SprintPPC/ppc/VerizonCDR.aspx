<%@ Page Title="Verizon CDR" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="VerizonCDR.aspx.vb" Inherits="VerizonCDR" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">

    <style type="text/css">
        
        .downloadVerCdrDiv {    
            height: 25px;
            margin-top: 10px;
        }
        
        .downloadVerCdrDiv input[type=submit] {
            float: right;
        }
        
    </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div id="mainDiv">
        <div id="verizonCDRGVDiv" style="overflow: scroll; overflow-y: hidden;">
            <asp:GridView ID="verizonCdrGV" runat="server" AutoGenerateColumns="true" Font-Size="11px" Width="100%"
                HeaderStyle-Wrap="false" RowStyle-Wrap="false"
                DataSourceID="verizonCdrGVSqlDataSource" AllowSorting="true" AllowPaging="true" PageSize="20">
            </asp:GridView>
            
            <asp:SqlDataSource ID="verizonCdrGVSqlDataSource" runat="server">
            </asp:SqlDataSource>
        </div>
        
        <div id="downloadVerCdrDiv" class="downloadVerCdrDiv" runat="server">
            <asp:Button ID="downloadVerCdrBtn" runat="server" Text="Download" />
        </div>

    </div>

</asp:Content>