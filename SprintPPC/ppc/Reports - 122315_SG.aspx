<%@ Page Title="Reports" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Reports.aspx.vb" Inherits="Reports" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" Runat="Server">

    <div id="mainDiv">
        
        <%--Note: Typically the TargetControlID is the element that triggers showing the popup
        on click. However, in this case we want to trigger on dropdown selection and not onclick.
        See the javascript at the end of the file that shows the popup.--%>
        <asp:ModalPopupExtender ID="modalShowProgress" runat="server" PopupControlID="showProgressPanel"
                        BackgroundCssClass="modalBackground" TargetControlID="showProgressPanel">
        </asp:ModalPopupExtender>

        <asp:Panel ID="showProgressPanel" runat="server" CssClass="showProgressModalPanel">
            <asp:UpdateProgress ID="reportGenerationProgress" runat="server">
                <ProgressTemplate>
                    <div>
                        <img alt="" id="imgLoading" src="images/loading-bars.gif" />
                    </div>
                    <div>
                        <span>Loading...</span>
                    </div>
                </ProgressTemplate>
            </asp:UpdateProgress>
        </asp:Panel>

        <div id="selectReportDiv" style="height: 30px;">
            <label>Please select a report:</label>&nbsp;
            <asp:DropDownList ID="selectReportDropdown" onchange="blockPage();" runat="server" AutoPostBack="true">
                <asp:ListItem Value="none">None</asp:ListItem>
                <asp:ListItem Value="minPerCycle">Minutes Per Cycle</asp:ListItem>
                <asp:ListItem Value="itemsPerOrder">Equipment Per Order</asp:ListItem>
                <asp:ListItem Value="unscrapedOrders">Unlinked Orders</asp:ListItem>
                <asp:ListItem Value="mdnWithoutOrders">MDNs Without Orders</asp:ListItem>
                <asp:ListItem Value="esnStatus">ESN Status</asp:ListItem>
                <asp:ListItem Value="verRpt">Verizon Last Days Calls</asp:ListItem>
                <asp:ListItem Value="totalMin">Total Minutes By Date</asp:ListItem>
            </asp:DropDownList>
            </div> 
        <div style="height: 30px;">
       <div id="dateRange" runat="server" visible ="false">     From:  <asp:TextBox ID="FromDate" runat="server"></asp:TextBox>
           To:  <asp:TextBox ID="ToDate" runat="server"></asp:TextBox>
           &nbsp;                    Cell #:  <asp:TextBox ID="CellNum" runat="server"></asp:TextBox>
           <asp:Button ID="go" runat="server" Text="Go" Width="30px" />
        </div> 
              
            </div>
        &nbsp;
        <div id="displayReportDiv" style="overflow: auto; min-height: 0%;">
            <asp:GridView ID="displayReportGridview" runat="server" AutoGenerateColumns="true"
                            AllowPaging="true" PageSize="20" RowStyle-Wrap="false" HeaderStyle-Wrap="false"
                            Width="100%" Font-Size="11px">
            </asp:GridView>
        </div>

        <div id="downloadBtnDiv" class="downloadBtnDiv" visible="false" runat="server">
            <input id="downloadReportBtn" type="button" onclick="preventMultiClick();" runat="server" value="Download" />
        </div>

        <asp:HiddenField ID="downloadTokenHdnFld" runat="server" />
        <asp:HiddenField ID="hdnIsCached" runat="server" />

    </div>

    <script type="text/javascript">

        var modalProgress = '<%= modalShowProgress.ClientID %>';
        var selectReportDropdown = document.getElementById('<%= selectReportDropdown.ClientID %>');
        var reportGv = document.getElementById('<%= displayReportGridview.ClientID %>');
        bindClickEventToPagingLinks();

        function bindClickEventToPagingLinks() {
            if (selectReportDropdown.value == "minPerCycle") {
                var rows = reportGv.getElementsByTagName('tr'),
                    pagingLinks = rows[rows.length - 1].getElementsByTagName('a');

                for (var i = 0; i < pagingLinks.length; i++) {
                    pagingLinks[i].onclick = blockPage;
                }

            }
        }

        function blockPage() {

            if (selectReportDropdown.value == "minPerCycle" || selectReportDropdown.value == "esnStatus" || selectReportDropdown.value == "verRpt") {
                showUpdateProgress();
            }

        }

        function showUpdateProgress() {
            
            var updateProgress = document.getElementById( '<%= reportGenerationProgress.ClientID %>' ),
                    imgDiv = updateProgress.getElementsByTagName( 'div' )[0],
                    progressImg = document.getElementById( 'imgLoading' );

            $find( modalProgress ).show();        // It will hide itself on page load.
            updateProgress.style.display = "inline";

            // This makes the animated gif work (IE).
            setTimeout( function () { progressImg.src = progressImg.src }, 1 );

        }

        function preventMultiClick() {
            if (selectReportDropdown.value == "minPerCycle") {
                var updateProgress = document.getElementById('<%= reportGenerationProgress.ClientID %>'),
                    imgDiv = updateProgress.getElementsByTagName('div')[0],
                    progressImg = document.getElementById('imgLoading'),
                    token = new Date().getTime(),
                    fileDownloadCheckTimer;

                document.getElementById('<%= downloadTokenHdnFld.ClientID %>').value = token;

                blockPage();

                fileDownloadCheckTimer = window.setInterval(function () {
                    // Assumes that this is the only cookie.
                    var cookieValue = document.cookie.split(';')[0].split('=')[1];
                    if (cookieValue == token) {
                        window.clearInterval(fileDownloadCheckTimer);
                        $find(modalProgress).hide();
                        // Delete the cookie.
                        document.cookie = 'fileDownloadToken=; expires=Thu, 01 Jan 1970 00:00:01 GMT;';
                    }
                }, 150);
            }
        }

    </script>

</asp:Content>

