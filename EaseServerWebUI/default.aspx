<%@ Page Language="C#" ResponseEncoding="utf-8" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Security.Principal" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" lang="en" dir="ltr">
<head>
    <title>(:</title>
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
</head>
<body>
    <font size="24">&#9786;</font>
    <!--
    <%
        Response.Write(DateTime.Now.ToString() + "\n");
        IPrincipal user = HttpContext.Current.User;
        if (user != null && user.Identity.IsAuthenticated)
        {
            string id = string.Format("{0} ({1}), is Administrator : {2}.",
               user.Identity.Name,
               user.Identity.AuthenticationType,
               user.IsInRole("administrators"));
            Response.Write(id);
        }
    %>
    -->
</body>
</html>
