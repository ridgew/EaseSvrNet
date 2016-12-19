<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" debug="true" >
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<title>易致接入服务器 - 控制面板</title>
<style type="text/css" media="all">
    @import url(css/desktop.css);
</style>
<script type="text/javascript" src="js/jquery.js"></script>
<script type="text/javascript" src="js/jquery.cookie.js"></script>
<script type="text/javascript" src="js/jquery.contextmenu.r2.js"></script>
<!-- 桌面分块处理Js Begin -->
<script type="text/javascript" src="js/Global.js"></script>
<script type="text/javascript" src="js/DesktopParts/FixedParts.js"></script>
<script type="text/javascript" src="js/DesktopParts/Desktop.js"></script>
<script type="text/javascript" src="js/DesktopParts/ObjectDD.js"></script>
<script type="text/javascript" src="js/DesktopParts/DesktopSeparator.js"></script>
<!-- 桌面分布处理Js End -->
<!--<script type="text/javascript" src="js/datapicker/datapicker.js"></script>-->
<script type="text/javascript" src="js/jQuery/getPlugin.js"></script>
<script type="text/javascript" src="js/mainMenu.js"></script>
<script type="text/javascript" src="js/openWindow.js"></script>
<script type="text/javascript" src="js/init.js"></script>
<script type="text/javascript" src="js/AjaxPublic.js"></script>
<script type="text/javascript" src="js/public.js"></script>

</head>
<body scroll="no">
<!--头部开始-->
<div id="header">
    <div class="headTop">
        <div class="headTopText"><a id="GRPZ" href="javascript:void(0)" title="个人配置">个人配置</a> | <a href="../Login.ashx/logout" title="退出系统">退出系统</a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</div>
    </div>
    <div class="headNav" id="menudiv">
        <div class="headNavStrip" id="main_MenuList"></div>
    </div>
</div>
<!--头部结束-->
<!--内容开始-->
<div id="content">
<!--左侧开始-->
    <div id="left" minWidth="200">      
	  <div class="leftTop" id="m_Top"><span class="sp02"></span><span class="sp01"></span>操作菜单列表</div>
	  <div class="leftCon" id="c_Tree"></div>
	  <div class="leftbottom" id="m_Bottom"><span class="sp03"></span><span class="sp01"></span>快捷菜单</div>
      <div class="leftCon" id="s_cut" style="display:none;" style="width:100%;height:91%;"></div>
	</div> 
    <div id="line">
        <div class="lineicoleft" id="iconCollapse"></div>
    </div>
    <div id="right" minWidth="1"><div style="height:450px;background-color:#fff">&nbsp;</div></div> 
<!--右侧结束-->
</div>
<!--内容结束-->
<!--底部开始-->
<div id="footer">
    <div class="footText">
	    <table border="0" cellspacing="1" class="listf">
			<tbody class="tb1">
			  <tr>
				<th class="th01" style="width:15%"><span>服务器：</span><font id="__btnHOST_NAME"></font></th>
				<th class="th02" style="width:10%"><span>用户名：</span><font id="__btnLOGIN_NAME"></font></th>
				<th class="th03" style="width:10%"><span>工号：</span><font id="__btnNO"></font></th>
				<th class="th04" style="width:30%"><span>角色：</span><font id="__btnGROUP_NAME"></font></th>
				<th class="th05" style="width:10%"><span>等待：</span><font id="__btnFREE_TIME">0分钟</font></th>
				<th class="th06" style="width:20%"><span>日期：</span><font id="__btnDATE"></font></th>
				<th class="th07" style="width:5%"><span class="spnew" id="btnShowInformation" title="消息提示" style="display:none;"></span></th>
			  </tr>
			  </tbody>
		</table>
	</div>
</div>
<!--底部结束-->

<!--消息提示-->
<div id="pnlToldPanel" style="width:300px; position:absolute; right:0px; bottom:20px; display:none; z-index:2">
  <div class="windTop">
    <div class="windTopInt">
      <h1 class="hwind"><span title="关闭" id="btnToldClose"></span><font id="lblToldTitle"></font></h1>
    </div>
  </div>
  <div class="windText">
      <div class="winda">
      <div class="windBox" id="pnlToldBody"></div>
	  <h2 class="hwind1"><a style="cursor:pointer;" id="btnToldHistory" title="历史消息">历史消息</a></h2>
	  </div>
  </div>
</div>
<div id="pnlToldPanelMask" style="width:300px; height:180px; display:none; position:absolute; right:0px; bottom:20px; z-index:1"><iframe src="about:blank" frameborder="0" width="100%" height="100%"></iframe></div>
<!--小时提示结束-->
<script type="text/javascript" src="js/Told.js"></script>
</body>
</html>