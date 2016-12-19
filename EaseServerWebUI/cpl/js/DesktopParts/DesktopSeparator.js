var Separator = function(cloneElId, isHorizontal, CmpOne, CmpTwo)
{
	var base = this;
	//获取元素的多边尺寸
	var getRect = function(e)
	{
		var t = e.offsetTop, l = e.offsetLeft, w = e.offsetWidth, h = e.offsetHeight;
		while (e=e.offsetParent) { t += e.offsetTop; l += e.offsetLeft; }; 
		return {top: t,left: l,width: w,height: h,bottom: t+h,right: l+w};
	};

	var objTarget = (typeof cloneElId=="string")?document.getElementById(cloneElId):cloneElId;
	var objCmpOne = (typeof CmpOne=="string")?document.getElementById(CmpOne):CmpOne;
	var objCmpTwo = (typeof CmpTwo=="string")?document.getElementById(CmpTwo):CmpTwo;
	var targetRect = getRect(objTarget);
	var Rect1 = getRect(objCmpOne), Rect2 = getRect(objCmpTwo);
	base.onCreatedShadow = function(objShadow) {};
	base.onDropCallBack = function(objDragEl) {};
	base.canDrag = function(e,dragObj,x,y) { return true;};
	base.canDrop = function(e,dragObj,x,y) { return true;};

	var createShadow = function() {
		var shadowObj = document.getElementById(objTarget.id + "_shadow");
		var blnAppend = false;
		if (!shadowObj)
		{
			blnAppend = true;
			shadowObj = objTarget.cloneNode(true);
			shadowObj.id = objTarget.id + "_shadow";
			shadowObj.style.cursor = (isHorizontal) ? "col-resize" : "row-resize";
			shadowObj.style.position = "absolute";
			shadowObj.style.opacity = '0.4';
			shadowObj.style.filter = 'alpha(opacity=40)';
			shadowObj.style.display = "block";
		}
		targetRect = getRect(objTarget);
		shadowObj.style.left = targetRect.left + "px";
		shadowObj.style.top = targetRect.top + "px" ;
		shadowObj.style.width = targetRect.width + "px" ;
		shadowObj.style.height = targetRect.height + "px" ;
		shadowObj.innerHTML = objTarget.innerHTML;
		base.onCreatedShadow(shadowObj);
		if (blnAppend) document.body.appendChild(shadowObj);
		return shadowObj;
	};

	var removeShadow = function() {
		document.body.removeChild(document.getElementById(objTarget.id + "_shadow"));
	};

	//获取重影真实对象
	base.getTarget = function() { return objTarget; }

	//宽度
	base.getWidth = function(obj, attr) {
		var val = obj.getAttribute(attr);
		return (val) ? parseInt(val) : obj.offsetWidth;
	};

	//高度
	base.getHeight = function(obj, attr) {
		var val = obj.getAttribute(attr);
		return (val) ? parseInt(val) : obj.offsetHeight;
	};

	base.getTotalWidth = function() { return objCmpOne.clientWidth + objCmpTwo.clientWidth + objTarget.clientWidth; }
	base.getTotalHeight = function() { return objCmpOne.clientHeight + objCmpTwo.clientHeight + objTarget.clientHeight; }

	base.initialize = function() 
	{
		var objShadow = createShadow();
		base.DDObject = new ObjectDragDrop(objShadow);
		base.DDObject.setOverlay(true);
		base.DDObject.moveStyle = (isHorizontal) ? ObjectDD.HMOVE : ObjectDD.VMOVE;	

		base.DDObject.isInMoveRect = function(newPosX,newPosY) 
		{
			
			if (isHorizontal)
			{
				var mw1 = base.getWidth(objCmpOne, "minWidth");
				var mw2 = base.getWidth(objCmpTwo, "minWidth");
				if (mw1==null){mw1 = 0;}
				if (mw2==null){mw2 = 0;}
				var nWidth = newPosX - Rect1.left;
				//return {x:true, y:false};
				return {x:(nWidth>=mw1 && nWidth<=(base.getTotalWidth() - mw2)), y:false};
			}
			else
			{
				var mh1 = base.getHeight(objCmpOne, "minHeight");
				var mh2 = base.getHeight(objCmpTwo, "minHeight");
				if (mh1==null)	{mh1 = 0;}
				if (mh2==null)	{mh2 = 0;}
				var nHeight = newPosY - Rect1.top;
				return {x:false, y:( nHeight>=mh1 && nHeight<=( base.getTotalHeight() - mh2)) };
			}
		}

		base.DDObject.canDrag = base.canDrag;

		base.DDObject.canDrop = function(e, x, y)
		{
			if (e) { if (!base.canDrop(e, objShadow, x, y))	return; }
			if (isHorizontal)
			{
				var nWidth = x - Rect1.left;
				objCmpOne.style.width = nWidth + "px";
				objCmpTwo.style.width = (base.getTotalWidth() - nWidth - objTarget.clientWidth ) + "px";
			}
			else
			{
				var nHeight = y - Rect1.top;
				objCmpOne.style.height = nHeight + "px";
				objCmpTwo.style.height = (base.getTotalHeight() - nHeight - objTarget.clientHeight ) + "px";
			}
			base.onDropCallBack(objShadow);
		}

		base.release = function(e) { base.DDObject.drop(e); }
	}

	base.release = function(e) { }

	base.resetShadow = function() {
		createShadow();
	}
}