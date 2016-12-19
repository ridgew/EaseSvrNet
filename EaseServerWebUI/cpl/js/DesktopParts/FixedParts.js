/*********************************
 $Header: /Gwsoft-Ease/admin/js/DesktopParts/FixedParts.js 1     09-05-19 9:36 Baiye $
------------
Ridge Wong @ 2008年8月28日
-----------
桌面其他固件
//@require('Global.js')
********************************/
WIMP.DesktopParts = {
	
	//递归初始化调用
	initialize:function(){
		for(var member in this)
		{
			if (typeof(this[member]) == "object")
			{
				for (var innerMember in this[member])
				{
					if (innerMember == "initialize" && typeof(this[member][innerMember]) == "function")
					{
						this[member][innerMember]();
					}
					else if (this[member][innerMember] != null && typeof(this[member][innerMember]) == "object")
					{
						if (typeof(this[member][innerMember].initialize) != "undefined")
						{
							this.initialize.call(this[member][innerMember]);
						}
					}
				}
			}
			else if(member == "initialize" && typeof(this[member]) == "function")
			{
				if (this != WIMP.DesktopParts) this[member]();
			}
		}
	},

	//桌面项的配置参数
	configuration:{
		
		//采用"."前缀的默认命名空间
		defaultNamespace:'WIMP.DesktopParts',
		
		items:[
			   
			//标题头配置
			{type:".Header", value:{BindID:'header',
					config:{height:65}
				}
			},
			
			//左栏配置
			{type:".LeftPanel", value:{BindID:'left', 
				config:{width:200, height:450},
				maxWidth:450, minWith:200 }
			},
			
			//分隔块配置
			{type:".Separator", value:{BindID:'line', collapseElementID:'iconCollapse',
				config:{width:9, height:450}
			   }
			},
			
			//操作取配置
			{type:".RightPanel", value:{BindID:'right',
					config:{height:450}
				}
			},
			
			//网页底部配置 
			{type:".Footer", value:{BindID:'footer',
					config:{height:30}
				}
			}
			
		],
		
		//获取特定特定的配置信息
		getConfig:function(typeFullName)
		{
			var item = null;
			for(var i=0, j= this.items.length; i<j; i++)
			{
				item = this.items[i];
				if (item.type.substr(0,1) == ".") item.type = this.defaultNamespace + item.type;
				if (item.type == typeFullName)
				{
					return item.value;	
				}
			}
			return null;
		}
		
	},
	
	//桌面容器
	Container:null, //WIMP.Desktop
	
	//1 页面头
	Header:{
			initialize:function() {
					this.Type = "WIMP.DesktopParts.Header";
					this.config = WIMP.DesktopParts.configuration.getConfig(this.Type);
			},
			
			//获取当前桌面可视高度的大小
			getTotalHeight:function() {
					var jBind = $("#" + this.config.BindID);
					var height = WIMP.DesktopParts.getTotalSize(jBind, true);
					return height;
				}
			
		},
		
	//2 左栏
	LeftPanel:{
		
		//菜单栏初始化
		initialize:function(){
				this.Type = "WIMP.DesktopParts.LeftPanel";
				this.config = WIMP.DesktopParts.configuration.getConfig(this.Type);			
			},

		//尺寸丈量
		Rect:{width:0, height:0 },

		setTotalHeight:function(height) {
			this.Rect.height = height;
			$("#" + this.config.BindID).css("height", height + "px");
		},
		
		setTotalWidth:function(width) {
			this.Rect.width = width;
			$("#" + this.config.BindID).css("width", width + "px");
		},

		getContentWidth:function() {
			if (this.Rect.width > 0)
			{
				return this.Rect.width;
			}
			else
			{
				return $("#" + this.config.BindID).width();
			}
		},

		getTotalWidth:function() {
			with(WIMP.DesktopParts)
			{
				if ($("#" + this.config.BindID).css("display") == "none")
				{
					return 0;
				}
				else
				{
					var offSetWidth = 0; //4=border:2px;
					if (this.Rect.width > 0)
					{
						return this.Rect.width + offSetWidth;	
					}
					else
					{
						return $("#" + this.config.BindID).width() + offSetWidth;
					}
				}
			}
		},
		
		getTotalHeight:function() {
			if (this.Rect.height > 0)
			{
				return this.Rect.height;
			}
			else
			{
				return $("#" + this.config.BindID).height();
			}
		},
		
			//菜单切换
		MenuParts:{
			//
			initialize:function(){
				this.Type = "WIMP.DesktopParts.LeftPanel.MenuParts";

				var topRefresh = $($("#m_Top").find("span").get(1));
				topRefresh.click(function(){//刷新树
					if (top.deskTabs.getFrameLeft()) top.deskTabs.getFrameLeft().contentWindow.location.reload();
				});

				var bottomRefresh = $($("#m_Bottom").find("span").get(1));
				bottomRefresh.click(function(){//刷新快捷菜单
					window.top.getQuickMenu();
				});
				
				var topBtn = $($("#m_Top").find("span").get(0));
				var bottomBtn = $($("#m_Bottom").find("span").get(0));
				topBtn.click(function()
				{
					if ($(this).attr("class") == "sp02")
					{
						$(this).removeClass("sp02").addClass("sp03"); // + 
						$("#c_Tree").hide();

						bottomBtn.removeClass("sp03").addClass("sp02"); // - 
						$("#s_cut").show();
						$("#m_Bottom").removeClass("leftbottom").addClass("leftTop");
					}
					else
					{
						$(this).removeClass("sp03").addClass("sp02"); // -
						$("#c_Tree").show();

						$("#m_Bottom").removeClass("leftTop").addClass("leftbottom");
						if (bottomBtn.attr("class") == "sp02") bottomBtn.removeClass("sp02").addClass("sp03"); // +
						$("#s_cut").hide();
					}
				});
				
				bottomBtn.click(function()
				{
					if ($(this).attr("class") == "sp02") // -
					{
						$(this).removeClass("sp02").addClass("sp03"); // +
						$("#s_cut").hide();

						if ($("#m_Top").css("display") == "block") //显示目录树
						{
							$("#c_Tree").show();
							topBtn.removeClass("sp03").addClass("sp02"); // -
						}
					}
					else
					{
						$(this).removeClass("sp03").addClass("sp02"); // - 
						$("#s_cut").show();

						$("#c_Tree").hide();
						if (topBtn.attr("class") == "sp02")
						{
							topBtn.removeClass("sp02").addClass("sp03"); // +
							$("#m_Bottom").removeClass("leftbottom").addClass("leftTop");
						}
					}
				});
				
			},
			
			//是否显示Frame菜单
			showMenuFrame:function(blnShow) {
				//alert('showMenu ' + blnShow);
				var menu = $("#m_Top");
				var totalHeight = WIMP.DesktopParts.LeftPanel.getTotalHeight();
				var topBtn = $($("#m_Top").find("span").get(0));
				var bottomBtn = $($("#m_Bottom").find("span").get(0));
				if (!blnShow)
				{
					menu.css("display", "none");
					$("#c_Tree").css("display", "none");
					topBtn.removeClass("sp02").addClass("sp03"); // + 
					bottomBtn.removeClass("sp03").addClass("sp02"); // -

					$("#m_Bottom").css("display", "block");
					$("#m_Bottom").removeClass("leftbottom").addClass("leftTop");
					$("#s_cut").css("height", (totalHeight-26*2) + "px");
					$("#s_cut").css("display", "block");
				}
				else
				{
					menu.css("display", "block");
					$("#c_Tree").css("display", "block");
					topBtn.removeClass("sp03").addClass("sp02"); // - 
					bottomBtn.removeClass("sp02").addClass("sp03"); // +

					$("#m_Bottom").css("display", "block");
					$("#m_Bottom").removeClass("leftTop").addClass("leftbottom");
					$("#s_cut").css("height", (totalHeight-26*3) + "px");
					$("#s_cut").css("display", "none");
				}
			},

			autoSize:function() 
			{
				with(WIMP.DesktopParts.LeftPanel)
				{
					var totalHeight = getTotalHeight();
					$("#c_Tree").css("height", (totalHeight-26*2) + "px");
					$("#c_Tree").css("width", getContentWidth() + "px");
					if ($("#m_Top").css("display") == "block")
					{
						$("#s_cut").css("height", (totalHeight-26*3) + "px");
					}
					else
					{
						$("#s_cut").css("height", (totalHeight-26*2) + "px");
					}
					$("#s_cut").css("width", getContentWidth() + "px");
				}
			}
			
		 }
		
		},
	
	//3 分隔条
	Separator:{
		
		initialize:function()
		{
				this.Type = "WIMP.DesktopParts.Separator";
				this.config = WIMP.DesktopParts.configuration.getConfig(this.Type);
				this.switchState = ($("#left").css("display") == "none") ? this.Switch.Left : this.Switch.Right;
				
				this.bindElement = $("#"+this.config.BindID); //拖拉改变尺寸支持
				this.bindCollapseElement = $("#"+this.config.collapseElementID);
				
				$(this.bindCollapseElement).css("margin-top", ($(this.bindElement).height()/2 - this.bindCollapseElement.height()/2) + "px")
				//.attr("title", (this.switchState == WIMP.DesktopParts.Separator.Switch.Right) ? "隐藏左栏" : "显示左栏")
				.mouseover(function()
				{
					if ($(this).attr("disabled") == "true") return;
					with(WIMP.DesktopParts.Separator)
					{
						if (switchState == Switch.Right){
							//$(this).attr("title", "隐藏左栏");
							$(this).addClass("lineicolefthight");
						} else {
							//$(this).attr("title", "显示左栏");
							$(this).addClass("lineicorighthight");	
						}
					}
				})
				.mouseout(function()
				{
					with(WIMP.DesktopParts.Separator)
					{
						if (switchState == Switch.Right){
							$(this).removeClass("lineicolefthight");
						} else {
							$(this).removeClass("lineicorighthight");
						}
					}
				})
				.click(function()
				{
					if ($(this).attr("disabled") == "true") return;
					WIMP.DesktopParts.Separator.toggle();
				});
				
		},
		
		//可选的切换状态
		Switch:{Left:0, Right:1},
		
		//切换回调函数
		toggleCallBack:null,
		
		//分隔条上的切换
		toggle:function(tCb){
			if (typeof(tCb) == "function") this.toggleCallBack = tCb;

			if (this.bindCollapseElement)
			{
				// lineicoleft
				// -> lineicoright : lineicorighthight
				// <- lineicoleft : lineicolefthight
				with(WIMP.DesktopParts.Separator)
				{
					var fixOffset = 2;
					if (switchState == Switch.Right){
						$("#left").hide();
						$("#right").css("margin-left", WIMP.DesktopParts.Separator.getTotalWidth() + "px");
						if ($(this.bindCollapseElement).hasClass("lineicoleft")) $(this.bindCollapseElement).removeClass("lineicoleft");
						$(this.bindCollapseElement).removeClass("lineicolefthight").addClass("lineicoright");
						switchState = Switch.Left;
					} else {
						$("#left").show();
						$("#right").css("margin-left", (WIMP.DesktopParts.Separator.getTotalWidth() + fixOffset + parseInt($("#left").css("width"))) + "px");
						if ($(this.bindCollapseElement).hasClass("lineicoright")) $(this.bindCollapseElement).removeClass("lineicoright");
						$(this.bindCollapseElement).removeClass("lineicorighthight").addClass("lineicoleft");
						switchState = Switch.Right;
					}
				}
				if (this.toggleCallBack != null) this.toggleCallBack($("#left").css("display"));
				WIMP.Desktop.resize();
			}
		},

		getTotalWidth:function() {
			return $("#" + this.config.BindID).width();
		},
		
		setTotalHeight:function(height) {
				this.bindElement.css("height", height + "px");
			},
		
		//箭头切换左右，右则隐藏左栏。
		switchTo:function(senum)
		{
			with(WIMP.DesktopParts)
			{
				this.switchState = (senum == this.Switch.Right) ? this.Switch.Left : this.Switch.Right;
			}
			this.toggle(this.toggleCallBack);
		},
	
		//是否可用
		disabled:function(blnEnable)
		{
			$(this.bindCollapseElement).attr("disabled", blnEnable);
		},
		
		//当前切换状态
		getSwitchState:function()
		{
			return this.switchState;
		},
		
		//自动修改分隔条的位置
		autoSize:function() 
		{
			$("#"+this.config.collapseElementID)
			.css("margin-top", parseInt($(this.bindElement).height()/2 - this.bindCollapseElement.height()/2) + "px")
		}

	},
	
	//4 操作区
	RightPanel:{
		
		initialize:function(){
				this.Type = "WIMP.DesktopParts.RightPanel";
				this.config = WIMP.DesktopParts.configuration.getConfig(this.Type);
				var panelId = this.config.BindID;
				
				//渲染标签
				$.getPlugin(WIMP.Plugins.DesktopTabsRender, function() {
					WIMP.UIHelper.RenderTabs.Render(panelId);
					WIMP.DesktopParts.setContext();
					WIMP.DesktopParts.RightPanel.IE6RightArrow.autoSize();
				});
			},
		
		//尺寸丈量
		Rect:{width:0, height:0 },

		setTotalHeight:function(height) {
				this.Rect.height = height;
				$("#" + this.config.BindID).css("height", this.Rect.height + "px");
			},
		
		setTotalWidth:function(width) {
			width = (WIMP.DesktopParts.LeftPanel.getTotalWidth() == 0) ? width : (width - 4); //4=border:2px;
			if ($.browser.msie && $.browser.version == "6.0")
			{
				width += 4;
				//window.status = "设置宽度为:"  + this.Rect.width + "  @ " + (+new Date());
				$("#" + this.config.BindID).css("width", this.Rect.width + "px")
					.css("position","absolute")
					.css("top", WIMP.DesktopParts.Header.getTotalHeight()).css("left",0);
			}
			else
			{
				$("#" + this.config.BindID).css("width", width + "px");
			}
			this.Rect.width = width;
		},

		//针对IE6的标签右导航修正
		IE6RightArrow: {

			BindElID:'scrollRight',
			
			//绑定位置自动调整
			autoSize:function() {
				if ($.browser.msie && $.browser.version == "6.0")
				{
					var el = document.getElementById(this.BindElID);
					if (el)
					{
						el.style.top = "0px";
						el.style.left = (WIMP.Desktop.getTotalWidth() - WIMP.DesktopParts.LeftPanel.getTotalWidth() - 26) + "px";
					}
				}
			}

		},
		
		//自动调整大小
		autoSize:function(){
				//console.log("右边容器重置大小！")
				if (typeof deskTabs != "undefined") deskTabs.resizeTo(this.Rect.width, this.Rect.height);
				if ($.browser.msie && $.browser.version == "6.0")
				{
					WIMP.DesktopParts.RightPanel.IE6RightArrow.autoSize();
				}
			}
	},
	
	//5 状态栏、脚栏
	Footer:{
		
		initialize:function() {
				this.Type = "WIMP.DesktopParts.Footer";
				this.config = WIMP.DesktopParts.configuration.getConfig(this.Type);
				
		},
		
		getTotalHeight:function() {
			var jBind = $("#" + this.config.BindID);
			return WIMP.DesktopParts.getTotalSize(jBind, true);
		 }
		
	},


	//获取整形数据
	getIntSize:function(n, retN) {
			return (isNaN(n)) ? retN : parseInt(n);
		},

	getTotalSize:function(jBind, isGetHeight)
	{
		var s = this.getIntSize;
		if (isGetHeight == true)
		{
			return jBind.height() 
				+ s(parseInt(jBind.css("padding-top")),0) + s(parseInt(jBind.css("padding-bottom")),0)
				+ s(parseInt(jBind.css("border-top")),0) + s(parseInt(jBind.css("border-bottom")),0)
				+ s(parseInt(jBind.css("margin-top")),0) + s(parseInt(jBind.css("margin-bottom")),0);
		}
		else
		{
			return jBind.width() 
				+ s(parseInt(jBind.css("padding-left")),0) + s(parseInt(jBind.css("padding-right")),0)
				+ s(parseInt(jBind.css("border-left")),0) + s(parseInt(jBind.css("border-right")),0)
				+ s(parseInt(jBind.css("margin-left")),0) + s(parseInt(jBind.css("margin-right")),0);
		}
	},

	//获取元素的多边尺寸
	getRect:function(e)
	{
		var t = e.offsetTop;
		var l = e.offsetLeft;
		var w = e.offsetWidth;
		var h = e.offsetHeight;
		while (e=e.offsetParent) { t += e.offsetTop; l += e.offsetLeft; }; 
		return {top: t,left: l,width: w,height: h,bottom: t+h,right: l+w};
	},

	getEl:function(id) {return document.getElementById(id); },

	setContext:function()
	{
		window.context = new Object();
		//活动标签
		context.activeTab = deskTabs.getActiveTab();

		//检查左栏是否是隐藏状态
		context.isLeftPanelHidden = function()
		{
			return (WIMP.DesktopParts.Separator.switchState == 0);
		};

		//调左frame窗口的函数
		context.callLeft = function(strFunName, args)
		{
			if (arguments.length<1) { alert("请指定函数名称！"); return null;}
			var fInvoke = context.invoke.curry(context.getFrameLeft().contentWindow)
				.curry(strFunName);
			if (arguments.length>1)
			{
				for (var i=1; i<arguments.length ; i++ )
				{
					fInvoke = fInvoke.curry(arguments[i]);
				}
			}
			fInvoke();
		};

		//调右frame窗口的函数
		context.callRight = function(strFunName, args)
		{
			if (arguments.length<1) { alert("请指定函数名称！"); return null;}
			var fInvoke = context.invoke.curry(context.getFrameRight().contentWindow)
				.curry(strFunName);
			if (arguments.length>1)
			{
				for (var i=1; i<arguments.length ; i++ )
				{
					fInvoke = fInvoke.curry(arguments[i]);
				}
			}
			fInvoke();
		};

		context.getFrameLeft = function()
		{
			return deskTabs.getFrameLeft();
		};

		context.getFrameRight = function()
		{
			return deskTabs.getFrameRight();
		};

		//调任意窗口的函数
		context.invoke = function(Win, strFunName, args)
		{
			var params = arguments;
			if (params.length < 2  || typeof(Win) != 'object' || typeof(strFunName) != 'string')
			{
				alert("函数调用错误，请指定需要调用的窗口对象及相关函数名字！");
			}
			else
			{
				if (!Win.hasOwnProperty(strFunName))
				{
					alert(strFunName + "在指定窗口对象中未找到！"); return;
				}
				var f = Win[strFunName];
				if (typeof(f) != "function")
				{
					alert("原型对象" + strFunName + "在指定窗口对象中不是函数(Function)！"); return;
				}
				if (params.length == 2)
				{
					f();
				}
				else
				{
					var evalFun = f.curry();
					alert(evalFun);
					for (var i=2; i<params.length ; i++) { evalFun = evalFun.curry(params[i]);}
					evalFun.call(Win);
				}
			}
		};

	}
	
}