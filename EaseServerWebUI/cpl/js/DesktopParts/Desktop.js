/*********************************
$Header: /Gwsoft-Ease/admin/js/DesktopParts/Desktop.js 1     09-05-19 9:36 Baiye $

Ridge Wong @ 2008年8月26日
********************************/
Function.prototype.curry = function() {
		if (!arguments.length) return this;
		var __method = this;
		var __args = Array.prototype.slice.call(arguments);
		return function() {
				return __method.apply(this, __args.concat(
						Array.prototype.slice.call(arguments)));
		};
};

Function.prototype.partial = function(){
		var fn = this, args = Array.prototype.slice.call(arguments);
		return function(){
		var arg = 0;
		for ( var i = 0; i < args.length && arg < arguments.length; i++ )
				if ( args[i] === undefined )
						args[i] = arguments[arg++];
		return fn.apply(this, args);
	};
};

//桌面控制封装
WIMP.Desktop = {
	
	//桌面初始化
	initialize:function(){
		WIMP.DesktopParts.initialize();
		WIMP.DesktopParts.Container = this;
	},
	
	getTotalWidth:function() {
		return Math.min($(document).width(), $(window).width());
	},
	
	//获取当前桌面可视高度的大小
	getTotalHeight:function() {
			return Math.min($(document).height(), $(window).height());
		},
	
	//需要动态调整尺寸的块
	autoSizeParts:[WIMP.DesktopParts.LeftPanel.MenuParts,
		 WIMP.DesktopParts.Separator,
		 WIMP.DesktopParts.RightPanel],
	
	//桌面改变尺寸
	resize:function(){
		
		var container = WIMP.Desktop;
		
		//设置主体桌面容器的大小(内容块高度自适应)
		with(WIMP.DesktopParts)
		{
			var contentHeight = container.getTotalHeight() - Header.getTotalHeight() - Footer.getTotalHeight();
			//$("#content").css("height", contentHeight + "px");
			LeftPanel.setTotalHeight(contentHeight);
			Separator.setTotalHeight(contentHeight);
			RightPanel.setTotalWidth(container.getTotalWidth() - LeftPanel.getTotalWidth() - Separator.getTotalWidth());
			RightPanel.setTotalHeight(contentHeight);

		}
		
		var part = null, method = null;
		for (var i=0, j= container.autoSizeParts.length; i<j; i++)
		{
			part = container.autoSizeParts[i];
			if (part.Type)
			{
				method = eval(part.Type + ".autoSize");
				if (typeof(method) == "function") method.call(part);
			}
		}
	},
	
	//切换左栏，隐藏参数：true,false
	collapseLeft:function(hide){
			$("#line_shadow").css("display", hide ? "none":"block");
			with(WIMP.DesktopParts)
			{
				Separator.switchTo(hide ? Separator.Switch.Left : Separator.Switch.Right); //指示可以朝右切换的状态
				RightPanel.autoSize();
			}
	}
	
};