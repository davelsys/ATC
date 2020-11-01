<%@ Page Title="Reports" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="Reports.aspx.vb" Inherits="Reports" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" Runat="Server">


 <style type="text/css">
           .btnCalendar
{
    BACKGROUND-POSITION: center 50%;
    background-color: Silver;
    BACKGROUND-ATTACHMENT: fixed;
    /*BACKGROUND-IMAGE: url(Images/smallcalendar.gif);*/
    BACKGROUND: transparent url(Images/smallcalendar.GIF) center center;
    BACKGROUND-REPEAT: no-repeat;
    WIDTH: 18px;
    HEIGHT: 19px;
    padding: 1px 1px 1px 1px;
}
     </style>

 <meta http-equiv="Page-Exit" content="progid:DXImageTransform.Microsoft.GradientWipe(Duration=0,Transition=5)" />
<style type="text/css">@import url(App_Themes/Aqua/theme.css);</style>

  <script type="text/javascript" src="calendar.js"></script>
<script type="text/javascript" src="calendar-en.js"></script>
<script type="text/javascript" src="calendar-setup.js"></script>
<script language="javascript" type="text/javascript" src="formatScripts.js"></script>
      
  
    <script type="text/javascript">


  function dateChanged(calendar) {

            if (calendar.dateClicked) {
                var y = calendar.date.getFullYear();
                var m = calendar.date.getMonth();
                var d = calendar.date.getDate();

                if (calendar.eventName == "from") {
                    //document.getElementById("FromLbl").textContent = m + 1 + "/" + d + "/" + y;
                    document.getElementById('<%= txtbFrom.ClientID%>').value = m + 1 + "/" + d + "/" + y;
                    document.getElementById('<%= txtFrom.ClientID%>').value = m + 1 + "/" + d + "/" + y;
                    //alert(document.getElementById('<%= txtFrom.ClientID%>').value);
                    calendar.callCloseHandler()
                }
                if (calendar.eventName == "to") {
                    document.getElementById('<%= txtbTo.ClientID%>').value = m + 1 + "/" + d + "/" + y;
                    document.getElementById('<%= txtTo.ClientID%>').value = m + 1 + "/" + d + "/" + y;
                    //document.getElementById("ToLbl").textContent = m + 1 + "/" + d + "/" + y;
                    calendar.callCloseHandler()
                }


                //document.getElementById("useCalendar").Value = "true";

               // __doPostBack('', '');

                calendar.callCloseHandler()
                //calendar.hide;
            }
  }
        function DateChanged()
        {
            document.getElementById('<%= txtFrom.ClientID%>').value = document.getElementById('<%= txtbFrom.ClientID%>').value;
            document.getElementById('<%= txtTo.ClientID%>').value = document.getElementById('<%= txtbTo.ClientID%>').value;
        }
        </script>
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
            <asp:DropDownList ID="selectReportDropdown"  runat="server" AutoPostBack="true"><%--onchange="blockPage(); "--%>
                <asp:ListItem Value="none">None</asp:ListItem>
                <asp:ListItem Value="minPerCycle">Minutes Per Cycle</asp:ListItem>
                <asp:ListItem Value="itemsPerOrder">Equipment Per Order</asp:ListItem>
                <asp:ListItem Value="unscrapedOrders">Unlinked Orders</asp:ListItem>
                <asp:ListItem Value="mdnWithoutOrders">MDNs Without Orders</asp:ListItem>
                <asp:ListItem Value="esnStatus">ESN Status</asp:ListItem>
                <asp:ListItem Value="mismatchedplans">Mismatched Plans</asp:ListItem>
                <asp:ListItem Value="verRpt">Verizon Audit Calls</asp:ListItem>
                <asp:ListItem Value="totalPlan">Totals Per Plan</asp:ListItem>
            </asp:DropDownList>
            </div> 
        <div style="height: 30px; width:100%">
       <div id="dateRange" runat="server" visible ="false" width="100%">  
           <table width="60%">
               <tr>
                   <td align="right">From:</td>
                   <td align="right" width="5%">
                      <asp:TextBox runat="server" ID="txtbFrom" Width="80px" onchange="DateChanged()"></asp:TextBox>
                   </td>
                   <td align="left">
                        <button id="showCalendar1" class="btnCalendar"  style="vertical-align:bottom" />
                   </td>
                   <td align="right">
                       To:
                   </td>
                   <td align="right"  width="5%">
                        <asp:TextBox runat="server" ID="txtbTo" Width="80px" onchange="DateChanged()"></asp:TextBox>
                   </td>
                   <td align="left">
                        <button id="showCalendar2"  class="btnCalendar" style="vertical-align:bottom"/>
                   </td>
                   <td>
                        <asp:Button ID="go" runat="server" text="GO"/>
                   </td>
               </tr>
           </table>  
           
                    
           <asp:HiddenField ID="txtFrom" runat="server" EnableViewState="false" />  <%--OnValueChanged="GetVerizonCdr" --%>
           <asp:HiddenField ID="txtTo" runat="server" EnableViewState="false"  /><%--OnValueChanged="GetVerizonCdr--%>
           <asp:HiddenField ID="useCalendar"  runat="server" EnableViewState="false"  /><%--onValueChanged="GetVerizonCdr"--%>
        </div>  
            <div id="selecttime" runat="server" visible ="false">
                <label>Please select a time range:</label>&nbsp;
                <asp:DropDownList ID="selecttimedropdown" runat="server" AutoPostBack="true" onchange="showUpdateProgress();">
               <asp:ListItem Value="0">Please Select A Time Range</asp:ListItem>
               <asp:ListItem Value="2">Last 2 hours</asp:ListItem>
               <asp:ListItem Value="12">Last 12 hours</asp:ListItem>
               <asp:ListItem Value="24">Last 24 hours</asp:ListItem>
           </asp:DropDownList>
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
         <asp:HiddenField ID="PlanName" runat="server" />

        <div id="displayTotalReportDiv" style="overflow: auto; min-height: 0%; width:80%; padding-left:85px"  runat="server">
            <div>&nbsp;</div>
            <asp:GridView ID="TotalDetailGv" runat="server" AutoGenerateColumns="true"
                            RowStyle-Wrap="false" HeaderStyle-Wrap="false"
                            Width="100%" Font-Size="11px"><%--AllowPaging="true" PageSize="20" --%>
            </asp:GridView>
            <div>&nbsp;</div>
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

            if (selectReportDropdown.value == "minPerCycle" || selectReportDropdown.value == "esnStatus" || selectReportDropdown.value== 'mismatchedplans')
            {
                showUpdateProgress();
            }
            if (selectReportDropdown.value == "verRpt") {
                if (confirm('This report may take a few minutes, would you like to continue?')==false) {
                    selecttime.hidden = true;
                }
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

        function getplan(plan) {
            document.getElementById('<%=PlanName.ClientID%>').value = plan;
            document.getElementById("ctl01").submit();
        }

  

        Calendar.setup(
       {
           //inputField: "txtFrom",
           inutField: "FromLbl",
           //            ifFormat: "%Y-%m-%d",
           button: "showCalendar1",
           // date: new Date(yr, mo, dy),
           onSelect: function () {
               this.eventName = "from";
               //this.eventName = "";
               dateChanged(this);
           }
       }
     );


        Calendar.setup(
        {
            inputField: "txtTo",
            //            ifFormat: "%Y-%m-%d",
            button: "showCalendar2",
            // date: new Date(yr, mo, dy),
            onSelect: function () {
                this.eventName = "to";
                dateChanged(this);
            }
        }
      );
        

       

    </script>

</asp:Content>

