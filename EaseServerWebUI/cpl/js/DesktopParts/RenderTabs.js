/*********************************
 $Header: /Gwsoft-Ease/admin/js/DesktopParts/RenderTabs.js 1     09-05-19 9:36 Baiye $
------------
Ridge Wong @ 2008年8月26日
-----------
//@require('Global.js')
********************************/
WIMP.namespace('WIMP.UIHelper.RenderTabs');

WIMP.UIHelper.RenderTabs.Render = function(EleId)
{
	var tabsConfig = {tabCls:'liOver liDef', viewCls:'rightBox', activeCls:'liActive liDef',
			inactiveCls:'liOver', tabHeight:27, tabMax:20,
			scrollBars:{scrollContainer:'tabStrip', left:['scrollLeft','tabscrollLeft', 'tabscrollLeft tabscrollLeft-disabled'],
			right:['scrollRight','tabscrollRight', 'tabscrollRight tabscrollRight-disabled']} 
	};
	//if (!$.browser.msie) tabsConfig.tabHeight = 26;
	var deskTabs = new AppTabview(EleId, tabsConfig);
	//创建标签头容器
	deskTabs.onCreateTabContainer = function(parentContainer, containerID){
		//alert('创建标签头容器');
		//return jQuery("<div class=\"rightTop\" id=\""+containerID+"\"><ul></ul></div>").appendTo(parentContainer);
		var pContainer = document.createElement('DIV');
			pContainer.id = "rightTop";
			pContainer.className = "rightTop";

		var sLeft = document.createElement("DIV");
			sLeft.id = "scrollLeft";
			sLeft.className = "tabscrollLeft tabscrollLeft-disabled";
			sLeft.style.display = "none";
			pContainer.appendChild(sLeft);

		var sRight = document.createElement("DIV");
			sRight.id = "scrollRight";
			sRight.className = "tabscrollRight tabscrollRight-disabled";
			sRight.style.display = "none";
			pContainer.appendChild(sRight);
		
		var tabStrip = document.createElement("DIV");
			tabStrip.id = "tabStrip";
			tabStrip.style.position = "relative";
			tabStrip.style.overflow = "hidden";
			tabStrip.style.whiteSpace = "nowrap";
			pContainer.appendChild(tabStrip);

		var ul = document.createElement('UL');
			ul.id = containerID;
			ul.style.position = "relative";
			ul.style.width = "50000px";
			ul.style.whiteSpace = "nowrap";
			ul.style.left = "0px";
			ul.style.top = "0px";
		var ret = tabStrip.appendChild(ul);
			parentContainer.appendChild(pContainer);

		return ret;
	};

	//创建标签
	deskTabs.onCreateTab = function(title, blnShowClose, containerEl, tabId){
		//alert('创建标签');
		var LI = document.createElement('LI');
			LI.id = tabId;
			LI.className = "liOver liDef";
			//LI.onmouseover = function(){ this.className = (this.className == "liOver") ? "liActive liDef" : "liOver";}
			//LI.onmouseout = function(){this.className = (this.className == "liOver") ? "liActive liDef" : "liOver";}

		if (blnShowClose == true)
		{
			var a = document.createElement('A');
			a.innerHTML = "<img src=\"images/space.gif\"  width=\"11px\" height=\"11px\"/>";
			a.className = "close";
			a.onclick = function() { deskTabs.removeTab(tabId); }
		    LI.appendChild(a);
		}
		var div = document.createElement('DIV');
		div.className = "out";
		var divInner = document.createElement('DIV');
			divInner.setAttribute("unselectable","on");
			divInner.setAttribute("style","-moz-user-select:none");
			divInner.className = "in02";
			divInner.style.cursor = "pointer"; //应用指针游标
		divInner.innerHTML = title;
		div.appendChild(divInner);
		LI.appendChild(div);
		return containerEl.appendChild(LI);
	};
	
	//当前菜单的frameID
	deskTabs.currentMenuFrameID = "";

	deskTabs.iframeOpen = function(title, url, useLeftFrameID)
	{
		var arrUrls = url.split('|')
		var callBack = function(tabWrap) {
			$("#m_Top").contents().not("[nodeType=1]").replaceWith(title);
			if ((arrUrls.length==2 && arrUrls[1] !="") || useLeftFrameID != null)
			{
				var leftMenuFrameId = "leftMenu_Frame" + tabWrap.Panel.id.replace(/[^\d]+/gi,"");
				if (useLeftFrameID != null) leftMenuFrameId = useLeftFrameID;
				if (document.getElementById(leftMenuFrameId))
				{
					$('#'+deskTabs.currentMenuFrameID).hide();
					$('#'+leftMenuFrameId).show();
					deskTabs.currentMenuFrameID = leftMenuFrameId;
				}
				else
				{
					if (deskTabs.currentMenuFrameID != "") $('#'+deskTabs.currentMenuFrameID).hide();
					$('<iframe frameborder="0" id="'+leftMenuFrameId+'" width="100%" height="100%" hspace="0" vspace="0" allowTransparency="true" scrolling="auto" style="margin:0px;padding:0px;" src="'+arrUrls[1]+'" ></iframe>').appendTo($("#c_Tree"));
					deskTabs.currentMenuFrameID = leftMenuFrameId;

					if (document.all) $('#'+leftMenuFrameId)[0].contentWindow.location.href = arrUrls[1];
				}

				var leftFrame = document.getElementById(leftMenuFrameId);
				if (useLeftFrameID != null)
				{
					var refReg = leftFrame.contentWindow.ReferObject;
					if (refReg == null || typeof refReg == "undefined") leftFrame.contentWindow.ReferObject = new Object();
					refReg = leftFrame.contentWindow.ReferObject;
					refReg[title] = url;
					tabWrap.SharedFrameID = useLeftFrameID;
				}

				//WIMP.DesktopParts.Separator.disabled(false);
				//WIMP.Desktop.collapseLeft(false);
				//$("#line_shadow").css("display", "block");
				WIMP.DesktopParts.LeftPanel.MenuParts.showMenuFrame(true);

				//标签销毁回调函数设置
				tabWrap.destroyCallback = function(tab) {
						var dTitle = tab.getTitle();
						//默认为当前对象菜单，其次查找共享菜单frame
						var targetleftFrame = document.getElementById("leftMenu_Frame" + tab.Panel.id.replace(/[^\d]+/gi,""));
						if (targetleftFrame == null) targetleftFrame = document.getElementById(tab.SharedFrameID);
						if (targetleftFrame)
						{
							var refReg = targetleftFrame.contentWindow.ReferObject;
							if (refReg != null)
							{
								var blnNoRefer = true;
								//减少引用窗口
								if (refReg[dTitle] != null) refReg[dTitle] = null;
								for (var item in refReg)
								{
									if (refReg[item] != null)
									{
										blnNoRefer = false;
										break;
									}
								}

								//检查创建Tab是否还存在
								var createTabEl = document.getElementById(EleId + "_Panel" + targetleftFrame.id.replace(/[^\d]+/gi,""));
								if (blnNoRefer)
								{
									//没有引用用且(创建Tab已不存在|只有其自身)
									if (createTabEl == null || tab.Panel.id == createTabEl.id)
									{
										//console.log("Remove" + targetleftFrame.id);
										targetleftFrame.parentNode.removeChild(targetleftFrame);
									}
								}
							}
							else
							{
								//console.log("Remove" + targetleftFrame.id);
								targetleftFrame.parentNode.removeChild(targetleftFrame);
							}
						}
				 };
			}
			else
			{
				//WIMP.DesktopParts.Separator.disabled(true);
				//WIMP.Desktop.collapseLeft(true);
				//$("#line_shadow").css("display", "none");
				WIMP.DesktopParts.LeftPanel.MenuParts.showMenuFrame(false);
			}
			WIMP.Desktop.resize();

			//begin---2008-9-3
			//每次窗口切换 定义点击活动iframe 窗口执行的函数，现有：隐藏右键菜单、隐藏菜单
			window.top.clickanywhere();
			//end----------------2008-9-3
		};
		this.open(title, true, false, arrUrls[0], callBack);
	};

	//获取左边菜单的iframe引用
	deskTabs.getFrameLeft = function() {
		var tabWrap = this.getActiveTab();
		if (tabWrap)
		{
			var idx = tabWrap.View.BindObject.id.replace(/[^\d]+/gi,"");
			var frameElement = document.getElementById("leftMenu_Frame" + idx);
			if (frameElement != null)
			{
				return frameElement;
			}
			return document.getElementById(deskTabs.currentMenuFrameID);
		}
		else
		{
			return null;
		}
	};

	//获取右边菜单的iframe引用
	deskTabs.getFrameRight = function() {
		var tabWrap = this.getActiveTab();
		if (tabWrap)
		{
			return document.getElementById(tabWrap.View.BindObject.id + "_frame");
		}
		else
		{
			return null;
		}
	};

	deskTabs.onTabNumAt = function(n) {
		if (n >= 10 && n < tabsConfig.tabMax)
		{
			alert("您已经打开"+n+"个标签窗口，打开更多将会导致系统运行缓慢！");
		}
		else if( n == tabsConfig.tabMax)
		{
			alert("最多允许打开" + tabsConfig.tabMax + "个标签页！");
		}
	};

	deskTabs.onTabEmpty = function() {
		WIMP.DesktopParts.LeftPanel.MenuParts.showMenuFrame(false);
	};

	//创建标签视图
//	deskTabs.onCreateTabview = function(viewId, blnAppend, url){
//		alert("创建标签视图");
//	};
	window.deskTabs = deskTabs.render();

	function readCookie(name) {
		var nameEQ = name + "=";
		var ca = document.cookie.split(';');
		for(var i=0;i < ca.length;i++) {
			var c = ca[i];
			while (c.charAt(0)==' ') c = c.substring(1,c.length);
			if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length,c.length);
		}
		return null;
	}
	function createCookie(name,value,days) {
		if (days) {
			var date = new Date();
			date.setTime(date.getTime()+(days*24*60*60*1000));
			var expires = "; expires="+date.toGMTString();
		}
		else var expires = "";
		document.cookie = name+"="+value+expires+"; path=/";
	}
	var ttl = unescape(readCookie('ttl'));
	var url1 = unescape(readCookie('url1'));
	var url2 = unescape(readCookie('url2'));
	//confirm('1.'+ttl+'.1');
	if (ttl!='1'&&url1!='1'&&ttl!=null&&url1!=null&&ttl!='null'&&url1!='null')
	{		
		deskTabs.iframeOpen(ttl,url1+(url2=='1'?'':'|'+url2));
		createCookie('ttl','1',1);
		createCookie('url1','1',1);
		createCookie('url2','1',1);
	}
};