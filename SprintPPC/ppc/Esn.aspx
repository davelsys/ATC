<%@ Page Title="" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Esn.aspx.vb" Inherits="Esn" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">




    <div style="overflow:auto; width: 930px; height: 100%;">
       
        <asp:GridView ID="esnvalGridview" runat="server" Width="98.3%" AutoGenerateColumns="false"
                        Font-Size="11px" ViewStateMode="Enabled"  ClientIDMode="static"
                        DataSourceID="esnvalGridviewDataSource"
                        AllowPaging="true" PageSize="20" AllowSorting="True" >
            <Columns>
                <asp:BoundField DataField="CellNum" HeaderText="CellNum" SortExpression="CellNum" />
                <asp:BoundField DataField="ppESN" HeaderText="PagePlusESN" SortExpression="ppESN" />
                <asp:BoundField DataField="OrderESN" HeaderText="OrderESN" SortExpression="OrderESN"/>
                <asp:BoundField DataField="Status" HeaderText="Status" SortExpression="Status" />
                <asp:BoundField DataField="DeviceMake" HeaderText="Device Make" SortExpression="DeviceMake" />
                <asp:BoundField DataField="DeviceModel" HeaderText="Device Model" SortExpression="DeviceModel" />
                <asp:BoundField DataField="DeviceState" HeaderText="Device State" SortExpression="DeviceState" />
                <asp:BoundField DataField="LastModified" HeaderText="Last Modified" SortExpression="LastModified"/>
            </Columns>
        </asp:GridView>

        <asp:SqlDataSource ID="esnvalGridviewDataSource" runat="server"
            ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
            ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>" > 
        </asp:SqlDataSource>

        <input type="hidden" id="selectedRowIndex" value="-1" />

    </div>
        <div id="downloadBtnDiv" class="downloadBtnDiv" runat="server" style="margin-right: 5px;">
        <input id="downloadESNBtn" type="button" runat="server" value="Download" />
    </div>


</asp:Content>

