<%@ Page Title="Manage ESN" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ManageESN.aspx.vb" Inherits="ManageESN" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div>
        
        <div id="displayEsnDiv" style="float: left; width: 52%;">
            <fieldset>
                <legend>Esn Numbers</legend>
                <asp:GridView ID="esnGridView" runat="server" AllowPaging="True" PageSize="20"
                    AllowSorting="True" AutoGenerateColumns="False"
                    DataSourceID="esnDataSource" Width="450px">
                    <Columns>
                        <asp:BoundField DataField="Serial#" HeaderText="Serial #" SortExpression="Serial#" />
                        <asp:BoundField DataField="ESN" HeaderText="ESN" SortExpression="ESN" />
                        <asp:BoundField DataField="International" HeaderText="International" SortExpression="International" />
                        <asp:BoundField DataField="CustomerPin" HeaderText="Customer Pin" SortExpression="CustomerPin" />
                        <asp:BoundField DataField="InsertedDate" HeaderText="Date Created" SortExpression="InsertedDate" />
                    </Columns>
                </asp:GridView>

                <asp:SqlDataSource ID="esnDataSource" runat="server" 
                    ConnectionString="<%$ ConnectionStrings:ppcConnectionString %>"
                    ProviderName="<%$ ConnectionStrings:ppcConnectionString.ProviderName %>"  >

                    <SelectParameters>
                        <asp:ControlParameter ControlID="serialNumSearchFld" Name="SerialNum" 
                                DefaultValue="%" Type="String" />

                        <asp:ControlParameter ControlID="esnSearchFld" Name="ESN" 
                                DefaultValue="%" Type="String" />

                        <asp:ControlParameter ControlID="intlSearchFld" Name="Intl" 
                                DefaultValue="%" Type="String" />

                        <asp:ControlParameter ControlID="cusPinSearchFld" Name="CustPin" 
                                DefaultValue="%" Type="String" />

                        <asp:QueryStringParameter Name="DateCreated" Type="String" />

                    </SelectParameters>

                </asp:SqlDataSource>

                <%--<div id="downloadEsnTableDiv" style="margin: 5px; width: 98.5%;">
                    <asp:Button ID="downloadEsnBtn" runat="server" style="float: right;" Text="Download" />
                </div>--%>

            </fieldset>
        </div>

        <div id="esnRightPnl" style="float: right; width: 45%;">
            <div id="uploadEsnDiv">
                <fieldset>
                    <legend>Upload Files</legend>
                    <asp:FileUpload ID="esnUploadControl" runat="server" />
                    <input type="button" runat="server" value="Upload" id="uploadEsnFile" style="float: right;" onserverclick="uploadEsnFile_Click" />
                    <br />
                    <asp:Label ID="uploadStatusLbl" runat="server" ForeColor="Red"></asp:Label>
                </fieldset>
            </div>

            <div id="searchEsnDiv">
                <fieldset class="searchEsnFieldset">
                    <legend>Search ESN</legend>

                    <div>
                        <span>
                            <label>Serial #:</label>
                            <asp:TextBox ID="serialNumSearchFld" runat="server"></asp:TextBox>
                        </span>
                    </div>

                    <div>
                        <span>
                            <label>ESN:</label>
                            <asp:TextBox ID="esnSearchFld" runat="server"></asp:TextBox>
                        </span>
                    </div>

                    <div>
                        <span>
                            <label>Intl:</label>
                            <asp:TextBox ID="intlSearchFld" runat="server"></asp:TextBox>
                        </span>
                    </div>

                    <div>
                        <span>
                            <label>Customer Pin:</label>
                            <asp:TextBox ID="cusPinSearchFld" runat="server"></asp:TextBox>
                        </span>
                    </div>

                    <div>
                        <span>
                            <label>Date Created:</label>
                            <asp:TextBox ID="dateCreatedSearchFld" runat="server"></asp:TextBox>
                        </span>

                        <span>
                            <asp:Button ID="searchEsnBtn" style="float: right;" runat="server" Text="Go" />
                        </span>
                    </div>

                </fieldset>
            </div>

        </div>

    </div>

</asp:Content>

