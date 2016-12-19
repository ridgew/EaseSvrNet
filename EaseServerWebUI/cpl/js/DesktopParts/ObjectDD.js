/**
* 类 名 称： DragDrop|DD
* 功能说明： 可拖动类
* 版权信息： CopyRight 2005-2006 JoeCom
* 创 建 人： JoeCom | MSN:juwuyi@hotmail.com | blog:http://hi.baidu.com/joecom
* 创建日期： 2006-07-19
* 修改记录： 1. 2006-07-21 加上scrollTop 和 scrollLeft的相对移动
			 2. 2006-07-25 加入moveStyle属性，增加水平移动和垂直移动的功能
			 3. 2006-07-25 加入isInMoveRect函数，增加范围移动功能
Ridge Wong	 4. 2008-09 重新修改与封装，增加部分判断时间和重构函数名，修改事件的绑定方式。
*/

//以下定义移动方向的常量
var ObjectDD = { FREEMOVE:0, //自由移动，没有限制
	HMOVE : 1, //水平移动，也就是左右移动
	VMOVE : 2 //垂直移动，也就是上下移动
};

function ObjectDragDrop(obj)
{
	var base = this;
	var addEvent = function(el, evname, func) {
		if (el.attachEvent) { // IE
			el.attachEvent("on" + evname, func);
		} else if (el.addEventListener) { // Gecko / W3C
			el.addEventListener(evname, func, true);
		} else {
			el["on" + evname] = func;
		}
	};

	var removeEvent = function(el, evname, func) {
		if (el.detachEvent) { // IE
			el.detachEvent("on" + evname, func);
		} else if (el.removeEventListener) { // Gecko / W3C
			el.removeEventListener(evname, func, true);
		} else {
			el["on" + evname] = null;
		}
	};

	var debugMode = false, createOverLay = false;
	base.moveStyle = ObjectDD.FREEMOVE ;
	base.DragObject = (typeof obj=="string")?document.getElementById(obj):obj;
	base.setOverlay = function(blSet) { createOverLay = blSet };
	base.canDrop = function(cx,cy){};
	base.canDrag = function(e, dragObj, x,y){ return true;};
	//offsetX:x的移动距离;offsetY:y的移动距离
	base.isInMoveRect = function(newPosX,newPosY){return {x:true,y:true}};

	var NoSelected = function() { return false; };
	base.DragObject.onmousedown = function(e)
	{
		var foo = base.DragObject, e = e||event;
		if( e.layerX )
		{ 
			foo.oOffset = {x:e.layerX, y:e.layerY }; 
		}
		else 
		{ 
			foo.oOffset = {x:e.offsetX, y:e.offsetY }; 
		}
		addEvent(document, "mousemove", base.drag);
		addEvent(document, "mouseup", base.drop);
		addEvent(document, "selectstart", NoSelected);
	};

	//获取在客户端的视觉区域
	base.getClientRect = function(e) {
		var doc = document.documentElement || document.body;
		var foo = base.DragObject, e=e||event;
		return {x:e.clientX - foo.oOffset.x + doc.scrollLeft,
			y:e.clientY - foo.oOffset.y + doc.scrollTop};
	};

	base.forceOverlay = function(blnRemove)
	{
		var layerid = "x-drag-overlay";
		var objChk = document.getElementById(layerid);
		if (objChk) 
		{
			if (blnRemove) document.body.removeChild(objChk);
			return;
		}
		else
		{
			if (blnRemove == true) return;
		}

		var el = document.createElement("DIV");
			el.id = layerid;
			el.setAttribute("unselectable","on");
			el.style.position = "absolute";
			el.style["-moz-user-select"] = "none";
			el.style.backgroundColor = "#F3F3F3";
			el.style.opacity = '0.2';
			el.style.filter = 'alpha(opacity=20)';

			if (base.DragObject.style.zIndex)
			{
				el.style.zIndex = parseInt(base.DragObject.style.zIndex) -1;
			}
			else 
			{
				el.style.zIndex = 20000;
			}
			el.style.top = "0px";
			el.style.left = "0px";
			el.style.width = "100%";
			el.style.height = "100%";
			el.innerHTML = '<iframe frameborder="0" width="0" height="0" allowTransparency="true" scroll="no" src="about:blank"></iframe>';
		  document.body.appendChild(el);
	};

	base.drag = function(e)
	{
		var foo = base.DragObject;
		var clientRect = base.getClientRect(e);
		var mv = base.isInMoveRect(clientRect.x, clientRect.y);
		if (base.canDrag(e, foo, clientRect.x, clientRect.y))
		{
			//创建遮罩层，以免激活其他页面控件
			if (createOverLay == true) base.forceOverlay();
			if (mv.x && base.moveStyle!=ObjectDD.VMOVE) foo.style.left = clientRect.x + "px";
			if (mv.y && base.moveStyle!=ObjectDD.HMOVE) foo.style.top = clientRect.y + "px";
		}
	}

	base.drop = function(e)
	{
		var clientRect = base.getClientRect(e);
		removeEvent(document, "mousemove", base.drag);
		removeEvent(document, "mouseup", base.drop); 
		removeEvent(document, "selectstart", NoSelected);
		base.canDrop(e, parseInt(base.DragObject.style.left), parseInt(base.DragObject.style.top));
		if (createOverLay == true) base.forceOverlay(true);
	}
}