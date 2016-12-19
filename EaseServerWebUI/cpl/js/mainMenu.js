//全局变量
var _mainMenu =
{
	//浏览器是否IE版本
	isIE: true,
	//菜单快捷键对应按键c:67，o:79，u:85，t:84，i:73，s:83，a:65，m:77，r:82如果菜单要增加或者减少，这里请相应对调整，这里的顺序和菜单的排列顺序必须保持一致
	_menuKey: [],
	//一级菜单是否展开
	topMenuTF: false,
	//二级菜单是否展开
	secMenuTF: false,
	//当前展开的菜单在按键数组中的位置
	actNmenuNum: 0,
	//当前展开二级菜单的一级菜单项的位置
	topactMenuNum: -1,
	//顶级菜单
	tmTF: true,
	//一级菜单
	mmTF: false,
	//二级菜单
	smTF: false,
	//延时检测并关闭菜单
	CloseMenuFun: function() {
		if (_mainMenu.tmTF)
			MainMenuClose();
		if (_mainMenu.mmTF)
			MainMenuClose();
		if (_mainMenu.smTF)
			$("#ChildRenList").html("").hide();
    }
};
//获取菜单数据
var tDat = null;
//获取主菜单、下拉菜单、快捷菜单、主菜单按键等数据
var mainMenuArray = new Array();
//主菜单按键
var _mKeys = new Array();

//获取菜单数据请求
function getMainMenus() {
	var _menuConfig = {url:"./service/1.3.5.1.1"};
	var _menuObj = new AJAXFunction(_menuConfig);
	_menuObj.SuFun = function(menu)
	{
		try
		{
			var _MainMenu = "";
			menu = menu.d.Data;
			for (var m = 0; m < menu.length; m ++)
			{
				if (typeof(menu[m].KeyCode) != "undefined" && menu[m].KeyCode != null && typeof(menu[m].MenuName) != "undefined" && menu[m].MenuName != null)
				{
					//按键
					_mKeys[_mKeys.length] = parseInt(menu[m].KeyCode);
					//主菜单
					var _emptyLI = "";
					if (_MainMenu != "")
						_emptyLI = "<li class=\"liLine\"><img src=\"images/ch_head_nav_bot_line.gif\" /></li>\n";
									
					_MainMenu += _emptyLI + "<li class=\"liOver\">\n"
					_MainMenu += "	<div class=\"out\">\n";
					_MainMenu += "		<div class=\"in\"><a style=\"cursor:pointer;\" title=\"" + menu[m].MenuName + "\">" + menu[m].MenuName + "</a></div>\n";
					_MainMenu += "	</div>\n";
					_MainMenu += "</li>\n";
									
					//alert(menu[m].Children);
					if (typeof(menu[m].Children) != "undefined" && menu[m].Children != null)
						mainMenuArray[mainMenuArray.length] = eval(getChildRenMenu(menu[m].Children));
					else
						mainMenuArray[mainMenuArray.length] = [];
				}
			}
			_MainMenu = "<ul>\n" + _MainMenu + "</ul>\n";
			$("#main_MenuList").html(_MainMenu);
			getQuickMenu();

			tDat = mainMenuArray;
			_mainMenu._menuKey = _mKeys;
			//加载主菜单鼠标事件
			init_MainMenUMouse();
		}
		catch (e)
		{
			alert("获取菜单数据出错！点击确定重新刷新本页！");
		}
	};
	_menuObj.SendFun();
}

//获取下拉菜单
function getChildRenMenu(pMenu) {
	var cm = "[";
	for (var i = 0; i < pMenu.length; i ++)
	{
		if (typeof(pMenu[i].MenuName) != "undefined" && pMenu[i].MenuName != null)
		{	
			var _url = "";
			if (typeof(pMenu[i].RightUrl) != "undefined")
				_url += (pMenu[i].RightUrl == null ? "" : pMenu[i].RightUrl) + "|";
			if (typeof(pMenu[i].LeftUrl) != "undefined")
				_url += pMenu[i].LeftUrl == null ? "" : pMenu[i].LeftUrl;
					
			cm += "['" + pMenu[i].MenuName + "','" + _url + "'";
					
			if (typeof(pMenu[i].Children) != "undefined" && pMenu[i].Children != null)
				cm += "," + getChildRenMenu(pMenu[i].Children) + "";
					
			if (i < pMenu.length - 1)
				cm += "],";
			else
				cm += "]";
		}
	}
	return cm + "]";
}

//查询快捷菜单
function getQuickMenu()
{
	$("#s_cut").html("　<img src=\"./images/adm_loading2.gif\" alt=\"快捷菜单加载中……\" />快捷菜单加载中……");
	var _menuConfig = {url:"./service/1.3.5.1.15"};
	var _menuObj = new AJAXFunction(_menuConfig,false);
	_menuObj.SuFun = function(menu)
	{
		menu = menu.d.Data;
		if (menu.length > 0)
		{
			var _quickMenu = "";
			for (var m = 0; m < menu.length; m ++)
			{
				if (typeof(menu[m].MenuName) != "undefined" && typeof(menu[m].LeftUrl) != "undefined" && typeof(menu[m].RightUrl) != "undefined")
				{
					var _emp = "　";
					if (_mainMenu.isIE)
						_emp = "";
					_quickMenu += "<li style=\"height:26px; line-height:26px;\">" + _emp + "<img src=\"./images/ch_right_ico01.gif\" alt=\"快捷菜单\" /><a href=\"javascript:deskTabs.iframeOpen('" + menu[m].MenuName + "','" + menu[m].RightUrl + "|" + menu[m].LeftUrl + "');\" title=\"" + menu[m].MenuName + "\">" + $.trim(menu[m].MenuName) + "</a></li>\n";	
				}
			}
			if (_quickMenu != "")
				$("#s_cut").html(_quickMenu);
			else
				$("#s_cut").html("　<img src=\"./images/adm_tip.gif\" alt=\"暂无快捷菜单！\" />暂无快捷菜单！");
		}
		else
			$("#s_cut").html("　<img src=\"./images/adm_tip.gif\" alt=\"暂无快捷菜单！\" />暂无快捷菜单！");
	};
	_menuObj.SendFun();
}


//获取浏览器是否IE版本
function isIE() {
	if ($.browser.msie)
		_mainMenu.isIE = true;
	else
		_mainMenu.isIE = false;
}


//初始化页面，获取所有IFRAME对象
function init_PageIFrames() {
	//获取最顶级窗口对象
	var topObj = null;
	if (_mainMenu.isIE)
		topObj = document.body;
	else
		topObj = document.documentElement;
	//绑定顶级窗口按键事件
	if (topObj)
		BindKeydown(topObj, true);
}


//绑定按键事件
function BindKeydown(Obj, topTF) {
	Obj.onkeydown = function(event) {
		if (_mainMenu.isIE) {
			event = window.event;
			if (!topTF)
				event = this.ownerDocument.parentWindow.event;
		}
		//获取按键
		var _key = event.keyCode || event.charCode;
		//按键事件_menuKey : [67,79,85,74,73,83,65,77]
		//如果ALT键在按下状态，组合键和浏览器快捷键有冲突，暂且屏蔽ALT键，用单键按键事件
		//如果CTRL，SHIFT,ALT键处于按下状态，则不执行以下操作
		if (!event.altKey && !event.ctrlKey && !event.shiftKey) {
			var _kNum = getIndexOfArr(_key, _mainMenu._menuKey);
			if (_kNum > -1) {
				init_ShowMenu(_kNum, true); //触发菜单事件
				_mainMenu.actNmenuNum = _kNum;
			}

			//方向键按键事件
			if (_mainMenu.topMenuTF || _mainMenu.secMenuTF || _kNum > -1) {
				if (_key == 37)
					init_LeftKey();
				else if (_key == 38)
					init_UpKey();
				else if (_key == 39)
					init_RightKey();
				else if (_key == 40)
					init_DownKey();
				else if (_key == 13)
					init_EnterKey();
				else if (_key == 27)
					init_ESCKey();
			}

			//防止方向键引起页面滚动，屏蔽方向键
			if (_key == 38 || _key == 40 || _key == 37 || _key == 39) {
				if (_mainMenu.isIE)
					event.returnValue = false;
				else
					event.preventDefault();
			}
		}
	}

	//input取消按键事件
	var inputs = Obj.getElementsByTagName("input");
	for (var i = 0; i < inputs.length; i++) {
		inputs[i].onkeydown = function(event) {
			if (!_mainMenu.isIE) {
				event.stopPropagation();
			} else {
				if (!topTF)
					event = this.ownerDocument.parentWindow.event;
				else
					event = window.event;
				event.cancelBubble = true;
			}
		}
	}
	//textarea取消按键事件
	var textareas = Obj.getElementsByTagName("textarea");
	for (var t = 0; t < textareas.length; t++) {
		textareas[t].onkeydown = function(event) {
			if (!_mainMenu.isIE) {
				event.stopPropagation();
			} else {
				if (!topTF)
					event = this.ownerDocument.parentWindow.event;
				else
					event = window.event;
				event.cancelBubble = true;
			}
		}
	}
	//目前暂时只针对input和textarea取消按键事件，如果有其他控件需要取消，请继续增加	
}

//获取元素在数组中的位置
function getIndexOfArr(k, Arr) {
	var i = -1;
	for (var n = 0; n < Arr.length; n++) {
		if (k == Arr[n]) {
			i = n;
			break;
		}
	}
	return i;
}

///////////////////////////////////////////////////////////////////////////////////////////
//鼠标事件
function init_MainMenUMouse() {
	$("div.headNavStrip ul li.liOver").each(function(i) {
		/*$(this).mouseover(function()
		{
		//更新当前菜单位置
		_mainMenu.actNmenuNum = i;
		//
		_mainMenu.tmTF = false;
		_mainMenu.mmTF = false;
		_mainMenu.smTF = true;

			//显示菜单
		init_ShowMenu(i,false);
		}).mouseout(function()
		{
		_mainMenu.tmTF = true;
		window.setTimeout(function(){_mainMenu.CloseMenuFun();},500);
		});*/

		//2008-09-10 菜单触发事件由鼠标移动改为点击触发。

		$(this).click(function(event) {
			if (!_mainMenu.topMenuTF || (_mainMenu.topMenuTF && _mainMenu.actNmenuNum != i)) {
				_mainMenu.actNmenuNum = i;
				init_ShowMenu(i, false);
			}
			else {
				MainMenuClose();
			}

			//
			if (!_mainMenu.isIE) {
				event.stopPropagation();
			} else {
				event = window.event;
				event.cancelBubble = true;
			}
		});
	});
}

///////////////////////////////////////////////////////////////////////////////////////////
//显示菜单
function init_ShowMenu(Mi, keyTF) {
	//_mainMenu.secMenuTF = false;
	//恢复顶级菜单状态
	$("div.headNavStrip ul li.liAct").each(function(l) { $(this).removeClass("liAct").addClass("liOver"); });
	//获取当前菜单对象
	var NowMenu = $($("div.headNavStrip ul li.liOver").get(Mi));
	//更改当前活动菜单状态
	NowMenu.removeClass("liOver").addClass("liAct");
	//如果之前已经有菜单打开，则先关闭已打开菜单
	if ($("#ChildRenList").length > 0) {
		$("#ChildRenList").hide();
		_mainMenu.secMenuTF = false;
	}
	if ($("#menulist").length > 0) {
		$("#menulist").hide();
		_mainMenu.topMenuTF = false;
	}
	//判断顶级菜单数据长度，是否超出索引
	if (tDat.length <= Mi) return;
	//获取当前活动菜单列表数据
	var _nowMenuData = tDat[Mi];
	//如果存在菜单项
	if (_nowMenuData.length > 0) {
		//如果菜单没有被创建，则首先创建
		if ($("#menulist").length <= 0) {
			$("<div></div>")
			.attr("id", "menulist")
			.addClass("menubigdiv")
			.append("<iframe src=\"\" style=\"opacity:0;filter:alpha(opacity=0); width:100%;\" id=\"tmpmain_Frame\"></iframe>")
			.append("<div class=\"menudiv\" id=\"mainMenu_ListDiv\"></div>")
			.appendTo("body");
		}
		//清空内容
		$("#mainMenu_ListDiv").html("");
		//填充菜单列表
		for (var m = 0; m < _nowMenuData.length; m++) {
			//菜单项
			var _Menup = $("<p></p>");
			//菜单名称及连接地址
			var _Title = _nowMenuData[m][0];
			var _Links = _nowMenuData[m][1];
			//如果该菜单项没有下级菜单
			if (_nowMenuData[m].length <= 2) {
				_Menup
				.html(_Title)
				.css("cursor", "pointer")
				.attr({ "title": _Title, "link": _Links, "submTF": false })
				.addClass("over")
				.click(function() {
					//增加菜单项目点击事件
					deskTabs.iframeOpen($(this).attr("title"), $(this).attr("link"));
					MainMenuClose();
				})
				.mouseover(function() {
					//如果有下级菜单项打开，则关闭
					if ($("#ChildRenList").length > 0) {
						$("#ChildRenList").hide();
						_mainMenu.secMenuTF = false;
					}
					$(this).removeClass("over").addClass("act");
				})
				.mouseout(function() {
					$(this).removeClass("act").addClass("over");
				});
			}
			else	//如果存在二级菜单
			{
				_Menup
				.html(_Title)
				.css("cursor", "pointer")
				.attr({ "title": _Title, "link": _Links, "submTF": true, "subIndex": Mi + ":" + m })
				.addClass("overbg")
				.click(function(event) {
					if (!_mainMenu.isIE) {
						event.stopPropagation();
					} else {
						event = window.event;
						event.cancelBubble = true;
					}
				})
				.mouseover(function() {
					SubMenuList($(this), false);
				})
				.mouseout(function() {
					$(this).removeClass("act02").addClass("overbg");
				});
			}
			_Menup.appendTo("#mainMenu_ListDiv");
		}
		//如果为按键触发菜单事件，则默认选中第一个菜单项
		if (keyTF) {
			var _FirstP = $($("#mainMenu_ListDiv p").get(0));
			var _SubmTF = _FirstP.attr("submTF");
			if (_SubmTF == "true")
				_FirstP.removeClass("overbg").addClass("act02");
			else
				_FirstP.removeClass("over").addClass("act");
		}
		//显示菜单
		var _Height = ((_nowMenuData.length * 24) + 6) + "px";
		$("#tmpmain_Frame").css("height", _Height);
		$("#menulist")
		.css({ "left": (NowMenu.offset().left) + "px", "top": (NowMenu.offset().top + 30) + "px", "height": _Height })
		.show()
		/*.mouseover(function()
		{
		_mainMenu.tmTF = false;
		_mainMenu.mmTF = false;
		_mainMenu.smTF = false;
		})
		.mouseout(function()
		{
		_mainMenu.smTF = true;
		_mainMenu.tmTF = true;
		_mainMenu.mmTF = true;
		window.setTimeout(function(){_mainMenu.CloseMenuFun();},1500);	
		})*/;
		$("#menulist")[0].focus();
		//更新一级菜单展开状态
		_mainMenu.topMenuTF = true;
		MenuClickHide();
	}
}

//显示二级菜单
function SubMenuList(topMObj, FirstActTF) {
	//更新当前项样式
	topMObj.removeClass("overbg").addClass("act02");
	//更新当前一级菜单中展开二级菜单项的位置
	var _actPNum = 0;
	var _topPNum = $("#mainMenu_ListDiv p").length;
	for (var ap = 0; ap < _topPNum; ap++) {
		if ($($("#mainMenu_ListDiv p").get(ap)).attr("class") == "act02") {
			_actPNum = ap;
			break;
		}
	}
	_mainMenu.topactMenuNum = _actPNum;

	//获取二级菜单项内容
	var _subArr = topMObj.attr("subIndex").split(":");
	var _subMenuData = tDat[parseInt(_subArr[0])][parseInt(_subArr[1])][2];
	//创建二级菜单
	if ($("#ChildRenList").length <= 0) {
		$("<div></div>")
		.attr("id", "ChildRenList")
		.addClass("childmenubig")
		.append("<iframe src=\"\" style=\"opacity:0;filter:alpha(opacity=0); width:100%;\" id=\"tmpchild_Frame\"></iframe>")
		.append("<div class=\"childmenudiv\" id=\"childMenu_ListDiv\"></div>")
		.appendTo("body");
	}
	$("#childMenu_ListDiv").html("");
	//填充二级菜单内容
	for (var sm = 0; sm < _subMenuData.length; sm++) {
		var _sTitle = _subMenuData[sm][0];
		var _sLinks = _subMenuData[sm][1];
		$("<p></p>")
		.html(_subMenuData[sm][0])
		.addClass("over")
		.css("cursor", "pointer")
		.attr({ "title": _sTitle, "link": _sLinks })
		.click(function() {
			//增加菜单项目点击事件
			deskTabs.iframeOpen($(this).attr("title"), $(this).attr("link"));
			MainMenuClose();
		})
		.mouseover(function() {
			$(this).removeClass("over").addClass("act");
		})
		.mouseout(function() {
			$(this).removeClass("act").addClass("over");
		})
		.appendTo("#childMenu_ListDiv");
	}

	//如果为有方向按键激活二级菜单，则第一个选须默认激活
	if (FirstActTF)
		$($("#childMenu_ListDiv p").get(0)).removeClass("over").addClass("act");

	//显示
	var _Height = ((_subMenuData.length * 24) + 6) + "px";
	$("#tmpchild_Frame").css("height", _Height);
	$("#ChildRenList")
	.css({ "left": (topMObj.offset().left + topMObj.width() + 5) + "px", "top": (topMObj.offset().top) + "px", "height": _Height })
	/*.mouseover(function()
	{
	_mainMenu.tmTF = false;
	_mainMenu.mmTF = false;
	_mainMenu.smTF = false;
	_mainMenu.secMenuTF = true;
	})
	.mouseout(function()
	{
	_mainMenu.tmTF = true;
	_mainMenu.mmTF = true;
	_mainMenu.smTF = true;
	_mainMenu.secMenuTF = false;
	window.setTimeout(function(){_mainMenu.CloseMenuFun();},500);
	})*/
	.show()
	;
	$("#ChildRenList")[0].focus();
	//更新二级菜单显示状态
	_mainMenu.secMenuTF = true;
}

//左按键事件
function init_LeftKey() {
	//如果二级菜单为展开状态
	if (_mainMenu.secMenuTF) {
		$("#childMenu_ListDiv").html("");
		$("#ChildRenList").hide();
		//更新一级菜单当前项显示样式
		if (_mainMenu.topactMenuNum > -1)
			$($("#mainMenu_ListDiv p").get(_mainMenu.topactMenuNum)).removeClass("overbg").addClass("act02");
		//更新二级菜单显示状态
		_mainMenu.secMenuTF = false;
	}
	else	//如果只有一级菜单展开，没有二级菜单展开
	{
		var Mi = _mainMenu.actNmenuNum;
		var _PreNum = Mi - 1;
		if (_PreNum < 0)
			_PreNum = _mainMenu._menuKey.length - 1;
		//更新当前展开的一级菜单的位置
		_mainMenu.actNmenuNum = _PreNum;
		//展开当前菜单
		init_ShowMenu(_PreNum, true);
	}
}

//上按键事件
function init_UpKey() {
	var _Obj = null;
	//如果二级菜单为展开状态
	if (_mainMenu.secMenuTF)
		_Obj = "childMenu_ListDiv";
	else
		_Obj = "mainMenu_ListDiv";

	bindUpKeyDown(_Obj, true);
}

function bindUpKeyDown(mDiv, upTF) {
	var _PNum = $("#" + mDiv + " p").length;
	var _ActPNum = $("#" + mDiv + " p.act").length || $("#" + mDiv + " p.act02").length;
	var _npNum = 0;
	//如果为向下按键
	if (!upTF)
		_npNum = -1;

	if (_ActPNum > 0) {
		for (var p = 0; p < _PNum; p++) {
			if ($($("#" + mDiv + " p").get(p)).attr("class") == "act" || $($("#" + mDiv + " p").get(p)).attr("class") == "act02") {
				_npNum = p;
				break;
			}
		}
	}
	var _upNum = _npNum - 1;
	if (_upNum < 0)
		_upNum = _PNum - 1;
	//如果为向下按键
	if (!upTF) {
		_upNum = _npNum + 1;
		if (_upNum > _PNum - 1)
			_upNum = 0;
	}

	$("#" + mDiv + " p").each(function() {
		var _subTF = $(this).attr("submTF");
		if (_subTF == "true")
			$(this).removeClass("act02").addClass("overbg");
		else
			$(this).removeClass("act").addClass("over");
	});

	var thisP = $($("#" + mDiv + " p").get(_upNum));
	var subTF = thisP.attr("submTF");
	if (subTF == "true") {
		thisP.removeClass("overbg").addClass("act02");
		//更新当前有二级菜单的项的位置
		//_mainMenu.topactMenuNum = _upNum;
	}
	else {
		thisP.removeClass("over").addClass("act");
	}
}


//右按键事件
function init_RightKey() {
	//获取右按键事件动作，true直接向右移动顶级菜单，false打开二级菜单
	var _RightAct = true;
	if (!_mainMenu.secMenuTF) {
		//获取一级菜单中处于活动状态的项
		if ($("#mainMenu_ListDiv p.act02").length > 0)
			_RightAct = false;
	}
	else {
		//如果二级菜单处于展开状态，向右移动则清空二级菜单内容，并且设置二级菜单展开状态为false
		$("#childMenu_ListDiv").html("");
		$("#ChildRenList").hide();
		_mainMenu.secMenuTF = false;
	}
	//动作
	if (_RightAct)	//直接向右移动顶级菜单
	{
		var Mi = _mainMenu.actNmenuNum;
		var _NextNum = Mi + 1;
		if (_NextNum > _mainMenu._menuKey.length - 1)
			_NextNum = 0;

		//更新展开一级菜单的位置
		_mainMenu.actNmenuNum = _NextNum;
		//展开一级菜单
		init_ShowMenu(_NextNum, true);
	}
	else		//打开二级菜单
	{
		var _SubObj = $($("#mainMenu_ListDiv p.act02").get(0));
		SubMenuList(_SubObj, true)
	}
}

//下按键事件
function init_DownKey() {
	var _Obj = null;
	//如果二级菜单为展开状态
	if (_mainMenu.secMenuTF)
		_Obj = "childMenu_ListDiv";
	else
		_Obj = "mainMenu_ListDiv";

	bindUpKeyDown(_Obj, false);
}


//回车键按键事件
function init_EnterKey() {
	//如果有二级菜单展开，则以二级菜单所选项为执行项
	var Obj = $($("#childMenu_ListDiv p.act").get(0));
	if (!_mainMenu.secMenuTF) {
		var _actNum = $("#mainMenu_ListDiv p.act").length;
		var _act02Num = $("#mainMenu_ListDiv p.act02").length;
		if (_act02Num == 0)
			Obj = $($("#mainMenu_ListDiv p.act").get(0));
		else
			Obj = $($("#mainMenu_ListDiv p.act02").get(0));
	}
	//执行操作
	if (Obj.attr("title") != "" && typeof (Obj.attr("title")) != "undefined" && Obj.attr("link") != "" && typeof (Obj.attr("link")) != "undefined") {
		deskTabs.iframeOpen(Obj.attr("title"), Obj.attr("link"));
		MainMenuClose();
	}
	else {
		init_RightKey();
	}
}


//ESC取消键按键事件
function init_ESCKey() {
	MainMenuClose();
}

//////////////////////////////////////////////////////////////////////////////////////////
//关闭菜单
function MainMenuClose() {
	$("#menulist").hide();
	$("#ChildRenList").hide();
	_mainMenu.topMenuTF = false;
	_mainMenu.secMenuTF = false;
	$("div.headNavStrip ul li.liAct").each(function(l) { $(this).removeClass("liAct").addClass("liOver"); });
}

function MenuClickHide() {
    if (top.deskTabs.getFrameRight())
	{
	    try {
	        $(top.deskTabs.getFrameRight().contentWindow.document).bind
		    ('click', function() {
		        MainMenuClose();
		    });
	    }
	    catch (e) { }
	}
	if (top.deskTabs.getFrameLeft()) {
        $(top.deskTabs.getFrameLeft().contentWindow.document).bind
	    ('click', function() {
	        MainMenuClose();
	    });
	}
}


//延时，等到页面加载完成后绑定事件到子窗口的click事件
function clickanywhere() {
	setTimeout("set_TimeOutFun();", 5000);
}

function set_TimeOutFun() {
	return false;
}

///////////////////////////////////////////////////////////////////////////////////////////
//页面加载执行
$(function() {

	//获取菜单等数据
	getMainMenus();

	//获取浏览器是否IE版本
	isIE();

	//初始化页面，获取当前页面对象
	init_PageIFrames();

	//鼠标点击关闭菜单
	$(document).click(function() { MainMenuClose(); });
});
