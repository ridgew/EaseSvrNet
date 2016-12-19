/*********************************
 $Header: /Gwsoft-Ease/admin/js/AppTabview.js 1     09-05-19 9:36 Baiye $
 $Author: wangqj $
 $Modtime: 09-05-19 9:24 $
 $Revision: 463 $

Ridge Wong @ 2008年8月26日
	Features:
	---------------------------
	1.标签浏览(附加)URL网页
	2.支持标签的关闭按钮显示、隐藏(不允许关闭)
	3.标签头双击关闭当前标签页，并激活前一个标签页
	4.开放创建标签头容器、标签头、标签视图接口
	5.支持配置标签的样式：视图类、标签头类、激活标签头类、非激活标签头类、标签头高度、最多允许打开的标签数目
	6.支持标签视图绑定回调函数
	7.支持删除所有标签页之后的回调函数 (2008年8月27日 10:10:52)
	8.增加创建iframe标签页的onresize空函数绑定 (2008-8-29)
	9.标签容器显示不下是增加标签头滚动，并自动激活当前显示标签头。 (2008-9-9)

*/
var AppTabview = function(ctid, objConfig)
{
	var $ = function(o){ return (typeof(o)=="object")?o:document.getElementById(o); };
	var $N = function(el, tagN,idx){return el.getElementsByTagName(tagN)[idx]; };
	var Env = {MSIE:navigator.userAgent.indexOf('MSIE')>=0?true:false,
		getStyleUnit:function(n){return (typeof n=="number" && Env.MSIE)?n:(n.toString().indexOf('%')!=-1) ? n:n+"px";}
	};
	var base = this; var ViewBind = $(ctid);
	//默认宽高设置
	var currentWith = 500, currentHeight = 350;
	var textPadding= 3, tbHeight = 22, maxOpenCount = 8;
	var closeImageHeight = 8, closeImageWidth = 8;

	var CLSConfig = {View:"dhtmlgoodies_aTab", TBar:"dhtmlgoodies_tabPane", TPActive:"tabActive", TPInactive:"tabInactive"};
	var scrollBarConfig = null, scrollBarBindEvent = false;
	var strictDocType = true;
	var regExp = new RegExp(".*MSIE ([0-9]\.[0-9]).*","g");
	var navigatorVersion = navigator.userAgent.replace(regExp,'$1');
	//IE6判断函数
	var isIE6 = function() { return (Env.MSIE && navigatorVersion == "6.0") };

	if (objConfig)
	{
		if (objConfig.viewCls)		{ CLSConfig.View = objConfig.viewCls; }
		if (objConfig.tabCls)		{ CLSConfig.TBar = objConfig.tabCls; }
		if (objConfig.activeCls)	{ CLSConfig.TPActive = objConfig.activeCls; }
		if (objConfig.inactiveCls)	{ CLSConfig.TPInactive = objConfig.inactiveCls; }

		if (objConfig.tabHeight)	{ tbHeight = objConfig.tabHeight; }
		if (objConfig.tabMax)		{ maxOpenCount = objConfig.tabMax; }
		/*asume: 
		scrollBars:{scrollContainer:'rightTop',left:['scrollLeft','tabscrollLeft', 'tabscrollLeft tabscrollLeft-disabled'], 
					right:['scrollRight','tabscrollRight', 'tabscrollRight tabscrollRight-disabled']} 
		*/
		if (objConfig.scrollBars)   { scrollBarConfig = objConfig.scrollBars;}
	}

	//当前最大索引种子
	var tabSeed = 0;
	//所有的标签数据引用
	var InternalTabs = new Object();
		InternalTabs.Tabs = new Array();
		InternalTabs.currentTab = null;

	//根据标签编号获取单个标签对象
	InternalTabs.getTab = function(pid)
	{
		for (var i=0; i<InternalTabs.Tabs.length; i++)
		{
			if (InternalTabs.Tabs[i].Panel.id==pid)
			{
				return InternalTabs.Tabs[i];
			}
		}
		return 	null;
	};

	//单个标签对象
	var TabWrap = function()
	{
		this.Panel = null;
		this.View = null;
		this.IsFront = false;
		//销毁回调函数
		this.destroyCallback = function(){};

		//获取主体内容显示的URL地址
		this.getURL = function() {
			if (this.Panel && this.Panel.getAttribute("url"))
			{
				return this.Panel.getAttribute("url").toLowerCase();
			}
			else
			{
				return "";
			}
		};

		//获取主体内容显示的title标题----2009-2-4
		this.getTitle = function() {
			if (this.Panel && this.Panel.getAttribute("title"))
			{
				return this.Panel.getAttribute("title").toLowerCase();
			}
			else
			{
				return "";
			}
		};

		//隐藏到后台
		this.bringBackground = function()
		{
			this.IsFront = false;
			setTabPanelStyle(this.Panel, CLSConfig.TPInactive, "images/tab_right_inactive.gif");
			this.View.BindObject.style.display = "none";
		};

		//提至前台
		this.bringFront = function()
		{
			if (InternalTabs.currentTab) 
			{
				if (InternalTabs.currentTab == this)
				{
					scrollIntoView(this.Panel); //0.39
					return;
				}
				else
				{
					InternalTabs.currentTab.bringBackground();
				}
			}

			InternalTabs.currentTab = this;
			if (this.IsFront == false)
			{
				setTabPanelStyle(this.Panel, CLSConfig.TPActive, "images/tab_right_active.gif");
				if(this.View != null)
				{
					var iframe = (InternalTabs.currentTab == null) ? null : InternalTabs.currentTab.View.BindObject.getElementsByTagName("IFRAME")[0];
					if (currentWith > 0 && parseInt(this.View.BindObject.style.width) != currentWith)
					{
						this.View.BindObject.style.width = Env.getStyleUnit(currentWith);
						if (iframe) iframe.width =  Env.getStyleUnit(currentWith);
					}
					if (currentHeight > 0 && parseInt(this.View.BindObject.style.height) != currentHeight)
					{
						this.View.BindObject.style.height = Env.getStyleUnit(currentHeight);
						if (iframe) iframe.height =  Env.getStyleUnit(currentHeight);
					}
					this.View.BindObject.style.display = "block";
					if (typeof(this.View.BindCallback) == "function") this.View.BindCallback(this);
				}
				this.IsFront = true;

				scrollIntoView(this.Panel); //0.39
			}

		}
	};

	var setTabPanelStyle = function(panelEl, clsPanel, srcImage)
	{
		panelEl.className = clsPanel;
		var img = panelEl.getElementsByTagName('IMG')[0];
		if (img && img.src)
		{
			if(img.src.indexOf('tab_')==-1)	img = panelEl.getElementsByTagName('IMG')[1];
			if (img && img.src) img.src = srcImage;
		}
	}

	//获取元素的多边尺寸
	var getRect = function(e)
	{
		var t = e.offsetTop;
		var l = e.offsetLeft;
		var w = e.offsetWidth;
		var h = e.offsetHeight;
		while (e=e.offsetParent) { t += e.offsetTop; l += e.offsetLeft; }; 
		return {top: t,left: l,width: w,height: h,bottom: t+h,right: l+w};
	};

	//根据URL地址查找标签
	var getTabByUrl=function(url)
	{
		for (var i=0; i<InternalTabs.Tabs.length ; i++ )
		{
			if (InternalTabs.Tabs[i].getURL() == url.toLowerCase())
			{
				return InternalTabs.Tabs[i];
			}
		}
		return null;
	};

	//根据Title查找标签
	var getTabByTitle=function(title)
	{
		for (var i=0; i<InternalTabs.Tabs.length ; i++ )
		{
			if (InternalTabs.Tabs[i].getTitle() == title.toLowerCase())
			{
				return InternalTabs.Tabs[i];
			}
		}
		return null;
	};

	//Entry
	base.render = function(Titles,acIndex,width,height,arrCloseBtn)
	{
		if (arguments.length == 0)
		{
			ViewBind.innerHTML = "";
			var boderWidth = 4;
			var rect = getRect(ViewBind);
			currentWith = rect.width-boderWidth;
			currentHeight = rect.height-tbHeight-boderWidth;
			createTabContainer(ViewBind, ViewBind.id + "_Panel_container");
			return base;
		}

		if (typeof width == "number") { currentWith = width; }
		if (typeof height == "number") { currentHeight = height - tbHeight; }
		//console.log("右侧总宽度：" + currentWith)
		//console.log("显示内容高度：" + currentHeight)
		if(!arrCloseBtn) arrCloseBtn = new Array();
		//创建标签头容器
		var tabDiv = createTabContainer(ViewBind, ViewBind.id + "_Panel_container");
		var cPanel,cView,cTab;
		var ctElements = ViewBind.childNodes;
		//创建标签头内导航条
		for(var i=0;i<Titles.length;i++)
		{
			//--Wrap begin--
			cPanel = createTab(Titles[i], arrCloseBtn[i], tabDiv, ViewBind.id + "_Panel" +  tabSeed);
			BindPanelEvent(cPanel);

			cView = new Object();
			var offSet = i+1;
			var m=0, n=0;
			var bindNode;
			while(n<ctElements.length && (ctElements[n].nodeType !=1 || m<=offSet))
			{
				if(ctElements[n].nodeType == 1) 
				{
					bindNode=ctElements[n];
					m++;
				}
				n++;
			}
			//console.log(bindNode);
			cView.CanDestroy = arrCloseBtn[i];
			cView.BindObject = transNode2Element(ctid + "_View" +  tabSeed, bindNode);
			cTab = new TabWrap();
			cTab.Panel = cPanel;
			cTab.View = cView;
			//console.log(cView.BindObject);
			if(i==acIndex)
			{
				setTabPanelStyle(cTab.Panel, 'tabActive', "images/tab_right_active.gif")
				cTab.IsFront = true;
				cView.BindObject.style.display = "block";
				InternalTabs.currentTab = cTab;
			}
			else
			{
				cView.BindObject.style.display = "none";
			}			
			cView.BindObject.style.width = Env.getStyleUnit(currentWith);
			cView.BindObject.style.height = Env.getStyleUnit(currentHeight);

			//更新种子
			tabSeed++;

			InternalTabs.Tabs[InternalTabs.Tabs.length] = cTab;
		}
		//console.log(InternalTabs);
		return base;
	};

	//改变显示尺寸大小
	base.resizeTo = function(width, height)
	{
		if (arguments.length > 0)
		{
			if (typeof width == "number" && parseInt(width) > 0) { currentWith = width; }
			if (typeof height == "number" && parseInt(height) > 0) { currentHeight = height - tbHeight; }
		}
		else
		{
			var rect = getRect(ViewBind);
			currentWith = rect.width-4;
			currentHeight = rect.height-tbHeight-4;
		}
		if (InternalTabs.currentTab != null)
		{
			InternalTabs.currentTab.View.BindObject.style.width = Env.getStyleUnit(currentWith);
			InternalTabs.currentTab.View.BindObject.style.height = Env.getStyleUnit(currentHeight);
			var iframe = InternalTabs.currentTab.View.BindObject.getElementsByTagName("IFRAME")[0];
			if (iframe)
			{
				iframe.width =  Env.getStyleUnit(currentWith);
				iframe.height =  Env.getStyleUnit(currentHeight);
				if (iframe.onresize) 
				{ 
					try { iframe.onresize(); } catch(e) {}; 
				}
			}
		}
		ViewBind.style.width = Env.getStyleUnit(currentWith);
		ViewBind.style.height = Env.getStyleUnit(currentHeight + tbHeight);
		ScrollBarReset();
	}

	//移除特定的标签
	base.removeTab = function(pid)
	{
		var tab = InternalTabs.getTab(pid);
		if (tab == null || tab.View.CanDestroy == false) return;
		if (tab.IsFront)
		{
			//激活前/或后一个标签
			var activeNode = tab.Panel.previousSibling || tab.Panel.nextSibling;
			if (activeNode)
			{
				var activeTab = InternalTabs.getTab(activeNode.getAttribute("id"));
				if (activeTab)	activeTab.bringFront();
			}
		}
		tab.View.BindObject.parentNode.removeChild(tab.View.BindObject);
		tab.Panel.parentNode.removeChild($(pid));
		try{ tab.destroyCallback(tab);} catch (e) {}
		delete tab;

		var newInternalTabs = new Array();
		for (var m=0; m<InternalTabs.Tabs.length ; m++ )
		{
			if (InternalTabs.Tabs[m].Panel.id!=pid)
			{
				newInternalTabs[newInternalTabs.length] = InternalTabs.Tabs[m]
			}
		}
		InternalTabs.Tabs = newInternalTabs;
		if (newInternalTabs.length == 0) 
		{
			tabSeed = 0;
			if (typeof(base.onTabEmpty) == "function") base.onTabEmpty();
		}
		ScrollBarReset();
	};

	//获取当前激活标签的引用
	base.getActiveTab = function()
	{
		return InternalTabs.currentTab;
	};

	//创建关闭图标按钮
	var createCloseBtn = function(parentContainer)
	{
		var imgBtn = document.createElement('IMG');
		imgBtn.src = 'images/close.gif';
		imgBtn.height = closeImageHeight + 'px';
		imgBtn.width = closeImageHeight + 'px';
		imgBtn.setAttribute('height',closeImageHeight);
		imgBtn.setAttribute('width',closeImageHeight);
		imgBtn.style.position='absolute';
		imgBtn.style.top = '6px';
		imgBtn.style.right = '0px';
		imgBtn.onmouseover = function(){ this.src = this.src.replace('close.gif','close_over.gif'); };
		imgBtn.onmouseout = function() { this.src = this.src.replace('close_over.gif','close.gif'); };
				
		parentContainer.innerHTML = parentContainer.innerHTML + '&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;';
		var deleteTxt = parentContainer.innerHTML+'';
		//移除标签处理
		imgBtn.onclick = function(evt)
		{ 
			var e=(evt)?evt:window.event;
			if (window.event) 
			{
				e.cancelBubble=true;
			} 
			else 
			{
				//e.preventDefault();
				e.stopPropagation();
			}
			//base.deleteTab(this.parentNode.innerHTML);
			base.removeTab(this.parentNode.parentNode.getAttribute("id"));
		};
		parentContainer.appendChild(imgBtn);
	};

	//Private 创建标签
	var createTab = function(title, blnShowClose, containerEl, tabId)
	{
		if (base.onCreateTab != null && typeof(base.onCreateTab) == 'function')
		{
			return base.onCreateTab(title, blnShowClose, containerEl, tabId);
		}
		
		var aTab = document.createElement('DIV');
		aTab.id = tabId;
		aTab.onmouseover = function()
		{
			if(this.className.indexOf(CLSConfig.TPInactive)>=0)
			{
				this.className='inactiveTabOver';
				var img = this.getElementsByTagName('IMG')[0];
				if(img.src.indexOf('tab_')<=0)img = this.getElementsByTagName('IMG')[1];
				img.src = 'images/tab_right_over.gif';
			}
		};

		aTab.onmouseout = function()
		{
			if(this.className ==  'inactiveTabOver')
			{
				this.className=CLSConfig.TPInactive;
				var img = this.getElementsByTagName('IMG')[0];
				if(img.src.indexOf('tab_')<=0)img = this.getElementsByTagName('IMG')[1];
				img.src = 'images/tab_right_inactive.gif';
			}
		};

		aTab.className=CLSConfig.TPInactive;
		containerEl.appendChild(aTab);

		var span = document.createElement('SPAN');
		span.setAttribute("unselectable","on");
		span.setAttribute("style","-moz-user-select:none");
		span.innerHTML = title;
		span.style.position = 'relative';
		aTab.appendChild(span);
		if(blnShowClose) { createCloseBtn(span); }
			
		var img = document.createElement('IMG');
			img.valign = 'bottom';
			img.src = 'images/tab_right_inactive.gif';
		// IE5.X FIX
		if((navigatorVersion && parseInt(navigatorVersion)<6) || (Env.MSIE && !strictDocType)){
			img.style.styleFloat = 'none';
			img.style.position = 'relative';	
			img.style.top = '4px'
			span.style.paddingTop = '4px';
			aTab.style.cursor = 'pointer';
		}	// End IE5.x FIX
		aTab.appendChild(img);

		
		return aTab;
	}

	//创建标签头容器
	var createTabContainer = function(parentContainer, containerID)
	{
		if (base.onCreateTabContainer !=null && typeof base.onCreateTabContainer == 'function')
		{
			return base.onCreateTabContainer(parentContainer, containerID);
		}

		var tabDiv = document.createElement('DIV');		
		var firstDiv = parentContainer.getElementsByTagName('DIV')[0];
		if (firstDiv) 
		{
			parentContainer.insertBefore(tabDiv,firstDiv);
		}
		else
		{
			parentContainer.appendChild(tabDiv);
		}
		tabDiv.id = containerID;
		tabDiv.className = CLSConfig.TBar;
		return tabDiv;
	};
	
	//创建标签视图
	var createTabview = function(viewId, blnAppend, url)
	{
		if (base.onCreateTabview != null && typeof base.onCreateTabview == 'function')
		{
			return base.onCreateTabview(viewId, blnAppend, url);
		}
		var div = document.createElement('DIV');
			div.className = CLSConfig.View;
			div.style.overflow = "auto";
			div.style.padding = "0px 0px 0px 0px";
			div.style.margin = "0px 0px 0px 0px";
			div.id = viewId;

		if (blnAppend == false)
		{
			//div.innerHTML = '<iframe frameborder="0" id="'+viewId+'_frame" width="100%" height="'+currentHeight+'" allowTransparency="true" scroll="auto" src="'+url+'" ></iframe>';
			var iframe = document.createElement("IFRAME");
			iframe.id = viewId+'_frame';
			iframe.frameBorder = iframe.style.margin = iframe.style.padding = iframe.style.border = 0;
			iframe.setAttribute("frameborder", "0");
			iframe.setAttribute("width", "100%");
			iframe.setAttribute("height", currentHeight);
			iframe.setAttribute("allowTransparency", "true");
			iframe.setAttribute("scroll", "auto");
			iframe.setAttribute("src", url);			
			iframe.onresize = function(){};
			div.appendChild(iframe);
		}
		else
		{
			//获取Ajax内容附加
			var fnUpdater = function(txt) { div.innerHTML = txt; };
			AjaxLite.getResponseText(url,"GET",null,fnUpdater);
		}
		return div;
	};

	//转换节点到Element引用
	var transNode2Element=function(idToApply, node)
	{
		if (node.nodeType != "1"){return null;}
		if (node.getAttribute("id"))
		{
			idToApply = node.getAttribute("id");
		}
		else
		{
			node.setAttribute("id", idToApply);
		}
		return document.getElementById(idToApply);
	};

	var getTabsTotalWidth = function() {
		var cPanl, tWidth = 0;
		for (var i=0; i<InternalTabs.Tabs.length ; ++i )
		{
			cPanel = InternalTabs.Tabs[i].Panel;
			tWidth += getRect(cPanel).width;
		}
		return tWidth;
	};

	var bindScrollLeftEvent = function(ect, elLeft, config)
	{
		elLeft.onclick = function() 
		{
			if (elLeft.className == config[1])
			{
				ect.scrollLeft -= 50;
			    ScrollBarReset();
			}
		};
	};

	var bindScrollRightEvent = function(ect, elRight, config)
	{
		elRight.onclick = function() 
		{
			if (elRight.className == config[1])
			{
				ect.scrollLeft += 50;
				ScrollBarReset();
			}
		};
	};

	//+ 0.39
	//显示激活标签头
	var scrollIntoView = function(tabPal)
	{
		if (scrollBarConfig != null && scrollBarConfig.scrollContainer && scrollBarConfig.left && scrollBarConfig.right)
		{
			var cPanl, tWidth = 0;
			for (var i=0; i<InternalTabs.Tabs.length ; ++i )
			{
				cPanel = InternalTabs.Tabs[i].Panel;
				tWidth += getRect(cPanel).width;
				if (cPanel == tabPal) break;
			}

			var esContainer = $(scrollBarConfig.scrollContainer);
			var escRect = getRect(esContainer);
			if (tWidth > escRect.width)
			{
				esContainer.scrollLeft = tWidth - escRect.width + 5;
			}
			else if (tWidth < escRect.width && esContainer.scrollLeft > 0)
			{
				esContainer.scrollLeft = 0;
			}
		}
	};

	//显示隐藏滚动条
	var ScrollBarReset = function() {
		if (scrollBarConfig != null && scrollBarConfig.scrollContainer && scrollBarConfig.left && scrollBarConfig.right)
		{
			var sLeft = $(scrollBarConfig.left[0]);
			var sRight = $(scrollBarConfig.right[0]);
			var esContainer = $(scrollBarConfig.scrollContainer);
			if (scrollBarBindEvent == false) 
			{
				bindScrollLeftEvent(esContainer, sLeft, scrollBarConfig.left);
				bindScrollRightEvent(esContainer, sRight, scrollBarConfig.right);
				scrollBarBindEvent = true;
			}
			var leftRect = getRect(sLeft), rightRect = getRect(sRight);
			var ctInnerWidth = currentWith - leftRect.width - rightRect.width;
			var tTabsWidth = getTabsTotalWidth();

			if (ctInnerWidth < tTabsWidth)
			{
				sLeft.style.display = "block";
				sRight.style.display = "block";
				//重新统计尺寸
				leftRect = getRect(sLeft); rightRect = getRect(sRight);
				ctInnerWidth = currentWith - leftRect.width - rightRect.width;
				sLeft.className = (esContainer.scrollLeft > 0) ? scrollBarConfig.left[1] : scrollBarConfig.left[2];
				sRight.className = (tTabsWidth - ctInnerWidth - esContainer.scrollLeft > 0) ? scrollBarConfig.right[1] : scrollBarConfig.right[2];
				esContainer.style.width = ctInnerWidth + "px";
			}
			else
			{
				esContainer.style.width = (currentWith-5) + "px";
				esContainer.scrollLeft = 0;
				sLeft.style.display = "none";
				sRight.style.display = "none";
			}
		}
	};

	//版定标签头的DOM事件
	var BindPanelEvent = function(cPanel)
	{
		//显示当前的标签，隐藏已显示的标签
		cPanel.onclick = function()
		{
			var tab = InternalTabs.getTab(this.id);
			if (tab) tab.bringFront();
		};

		cPanel.ondblclick = function(event)
		{
			base.removeTab(this.id);
		};
	};

	//创建事件API Public 扩展
	/*
		*.onCreateTabContainer = function(parentContainer, containerID){};
		*.onCreateTab = function(title, blnShowClose, containerEl, tabId){};
		*.onCreateTabview = function(viewId, blnAppend, url){};
		*.onTabNumAt = function(n){};
	*/
	base.onCreateTabContainer = null;
	base.onCreateTab = null;
	base.onCreateTabview = null;
	base.onTabEmpty = null; //标签为空时的回调函数
	base.onTabNumAt = null; //标签数目达到一定数目时调用

	//Append or Iframe Open
	base.open = function(title, blnShowClose, blnAppend, url, callBack)
	{
		//var tabExistUrl = getTabByUrl(url);
		//从通过URL判断窗口是否存在修改为通过标题判断窗口是否存在
		var tabExist = getTabByUrl(url);//getTabByTitle(title);
		if (tabExist)
		{
			if (InternalTabs.currentTab != tabExist)
			{
				tabExist.bringFront();
			}
			else
			{
				scrollIntoView(tabExist.Panel); //0.39
			}
//			if (!tabExistUrl)//如果该标签页打开的URL有变化，则重新加载数据 2009-2-4
//			{							
				tabExist.Panel.setAttribute("url", url.toLowerCase());
				if (document.all) tabExist.View.BindObject.getElementsByTagName("IFRAME")[0].contentWindow.location.href = url;
				else tabExist.View.BindObject.getElementsByTagName("IFRAME")[0].src = url;
//			}
		}
		else
		{
			if (base.onTabNumAt != null) base.onTabNumAt(InternalTabs.Tabs.length + 1);
			if(maxOpenCount !=0 && InternalTabs.Tabs.length >= maxOpenCount) 
			{  
				if (base.onTabNumAt == null) 
				{ 
					alert("最多允许打开" + maxOpenCount + "个标签页！"); 
				}
				else
				{
					base.onTabNumAt(InternalTabs.Tabs.length);
				}
				return;
			}
			var pContainer = $(ViewBind.id + "_Panel_container");
			var cPanel = createTab(title, blnShowClose, pContainer, ViewBind.id + "_Panel" +  tabSeed);
			cPanel.setAttribute("url", url.toLowerCase());
			cPanel.setAttribute("title", title.toLowerCase());
			BindPanelEvent(cPanel);

			var div = createTabview(ViewBind.id + "_View" +  tabSeed, blnAppend, url);
			var cView = new Object();
			cView.CanDestroy = blnShowClose;	//是否允许销毁视图和标签对象
			cView.BindCallback = callBack; //视图绑定回调函数
			cView.BindObject = ViewBind.appendChild(div);

			//解决IE下iframe.src指向与实际显示内容不一致的问题
			if (document.all) $(ViewBind.id+'_View'+tabSeed+'_frame').contentWindow.location.href = url;

			//新标签
			var cTab = new TabWrap();
			cTab.Panel = cPanel;
			cTab.View = cView;
			cView.BindObject.style.display = "block";
			InternalTabs.Tabs[InternalTabs.Tabs.length] = cTab;
			cTab.bringFront();
			
			//显示左右滚动条
			ScrollBarReset();

			tabSeed++;
		}

	};

	//URL编码
	var encodeURI=function(str)
	{
		var returnString;
		returnString=escape(str);
		returnString=returnString.replace(/\+/g,"%2B");
		return returnString;
	};

	//AjaxLite
	var AjaxLite = 
	{
	  
	  getTransport: function() {
		 if (typeof XMLHttpRequest != "undefined")
		 {
			 return new XMLHttpRequest();
		 }
		 else
		 {
			var xmlhttp = null;
			try 
			{
				xmlhttp = new ActiveXObject("Msxml2.XMLHTTP");
			} 
			catch (e) 
			{
				try 
				{
					xmlhttp = new ActiveXObject("Microsoft.XMLHTTP");
				}catch (err) {}
			}
			return xmlhttp;
		 }
	  },
	  
	  getResponseText:function(URL,Method,Content,Updater)
	  {
		   var ajax = this.getTransport();
				ajax.open(Method, URL, false);
				ajax.setRequestHeader("Content-Type","application/x-www-form-urlencoded");
			ajax.onreadystatechange = function() 
		    { 
				if (ajax.readyState == 4 && ajax.status == 200) 
				   { 
					if (typeof(Updater) == "function")
					{
						delete ajax;
						Updater(ajax.responseText);
					}
				}
				else if (ajax.readyState == 4 && ajax.status != 200)
			   {
					delete ajax;
					//alert("请求失败了，返回代码："+ajax.status);
			   }
			}
			ajax.send(Content);
	  }

	}


}