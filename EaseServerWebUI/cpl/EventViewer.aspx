<% @Page Language="C#" EnableEventValidation="false" Debug="true" %>
<% @Import Namespace="System.Diagnostics" %>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>系统事件日志查看器</title>
    <meta http-equiv="Pragma" content="no-cache" />
    <meta http-equiv="Cache-Control" content="no-cache" />
    <meta http-equiv="Expires" content="0" />
    <style type="text/css">
        *
        {
            font-size: 9pt;
        }
    </style>
</head>

<script language="C#" runat="server">
void Page_Load(Object sender, EventArgs e) 
{
    if (!Page.IsPostBack)
	{
		try
		{
			foreach (EventLog objEventLog in EventLog.GetEventLogs("."))
			{
                eventlogs.Items.Add(new ListItem(objEventLog.LogDisplayName,objEventLog.Log));
			}
		}
		catch(Exception ee)
		{
		   Debug.WriteLine(ee.Message);
		}
		BindGrid(null);
	}
}

void LogGrid_Change(Object sender, DataGridPageChangedEventArgs e) 
{
  LogGrid.CurrentPageIndex = e.NewPageIndex;
  BindGrid(eventlogs.Items[eventlogs.SelectedIndex].Value.ToString());
}
 
void BindGrid(string src) 
{
  EventLog aLog = new EventLog();
  aLog.Log = src ?? "System";
  aLog.MachineName = ".";
  LogGrid.DataSource = aLog.Entries;
  
   //Response.Write(LogGrid.CurrentPageIndex);
  //Response.Write("<br/>");
  //Response.Write(LogGrid.PageCount);
  //Response.End();
  
  if (LogGrid.CurrentPageIndex >=0)
  {
       if (LogGrid.CurrentPageIndex > LogGrid.PageCount)
        LogGrid.CurrentPageIndex = LogGrid.PageCount - 1; 
        
    if (LogGrid.CurrentPageIndex<0)
         LogGrid.CurrentPageIndex = 0;
         
     LogGrid.DataBind();
  }
  
}

void refinesearch_click(object sender, System.EventArgs e)
{
    LogGrid.CurrentPageIndex = 0;
    BindGrid(eventlogs.Items[eventlogs.SelectedIndex].Value.ToString());
}

</script>

<body bgcolor="#ffffff">
    <form runat="server">
    <table>
        <tr>
            <td width="100%" align="right">
                选择事件类型：<asp:DropDownList ID="eventlogs" runat="server" />
                <asp:TextBox ID="machinename" runat="server" Width="75px" Visible="False" />
                <asp:Button ID="refinesearch" Text="查看" runat="server" OnClick="refinesearch_click" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:DataGrid ID="LogGrid" runat="server" AllowPaging="True" PageSize="20" PagerStyle-Mode="NumericPages"
                    PagerStyle-HorizontalAlign="Right" PagerStyle-NextPageText="Next" PagerStyle-PrevPageText="Prev"
                    OnPageIndexChanged="LogGrid_Change" BorderColor="Black" BorderWidth="1px" CellPadding="3"
                    Font-Name="Verdana" Font-Size="9pt" HeaderStyle-BackColor="#aaaadd" AutoGenerateColumns="False"
                    Font-Names="Verdana">
                    <PagerStyle Mode="NumericPages" NextPageText="Next" PrevPageText="Prev" HorizontalAlign="Right">
                    </PagerStyle>
                    <Columns>
                        <asp:TemplateColumn HeaderText="类型">
                            <ItemTemplate>
                                <img src="icon/<%#DataBinder.Eval(Container.DataItem, "EntryType")%>.png" />
                            </ItemTemplate>
                        </asp:TemplateColumn>
                        <asp:TemplateColumn HeaderText="发生时间">
                            <ItemTemplate>
                                <asp:Label ID="Time" runat="server"><%#DataBinder.Eval(Container.DataItem,"TimeGenerated")%></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateColumn>
                        <asp:BoundColumn HeaderText="来源" DataField="Source" />
                        <asp:TemplateColumn HeaderText="消息" FooterStyle-Wrap="true">
                            <ItemTemplate>
                                <%#DataBinder.Eval(Container.DataItem, "Message")%>
                            </ItemTemplate>
                            <FooterStyle Wrap="True"></FooterStyle>
                        </asp:TemplateColumn>
                        <asp:BoundColumn HeaderText="事件ID" DataField="EventID" />
                    </Columns>
                    <HeaderStyle BackColor="#AAAADD"></HeaderStyle>
                </asp:DataGrid>
            </td>
        </tr>
    </table>
    </form>
</body>
</html>
