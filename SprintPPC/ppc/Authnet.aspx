<%@ Page Title="" Language="VB" AutoEventWireup="false" CodeFile="Authnet.aspx.vb" Inherits="Authnet" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>

    <script>
        function init() {
            //alert('<%=strToken%>')
            //alert(document.getElementById("Token").value);
            form1.submit();
           // alert("hi");
        }
    </script>
</head>
<body onload="init();">

    <form id="form1" action="https://test.authorize.net/profile/manage" method="post">
   
    
<input type="hidden" name="Token" id="Token" 
value="<%=strToken%>"
/>

<%--<asp:Button ID="btnPost" runat="server"  Text="click" />
<asp:Button ID="btnSubmit" runat="server" Text="click me"  PostBackUrl="https://test.authorize.net/profile/manage" />--%>
<%--

<button type="submit">Click Me!</button> --%>


    </form>
   <%-- <script>
        alert('before submit');
        document.form1.submit
        alert('after submit');
    </script>--%>
</body>
</html>
