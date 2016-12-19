//如果调用执行不成功，请在top 前面加上 window,即： window.top.fn_add_div('y.html','','','600px','500px');


//打开一个窗口
//示例：top.fn_add_div('y.html','100px','100px','600px','500px');
// top.fn_add_div('y.html','','','600px','500px');
//说明：如果左边距和右边距不指定则自动居中
function fn_del_div() {
	$('#temp_Over_Lay').hide();
	$('#temp_op_container').hide();
	$('#tmp_opt_iframe')[0].src = 'about:blank';
	$('#tmp_opt_iframe')[0].contentWindow.location.href = 'about:blank';
}

function fn_del_div1() {
	$('#temp_Over_Lay1').hide();
	$('#temp_op_container1').hide();
	$('#tmp_opt_iframe1')[0].src = 'about:blank';
	$('#tmp_opt_iframe1')[0].contentWindow.location.href = 'about:blank';
}

function fn_add_div1(url, left, top, width, height, title, callback) {
	width = (width + 'px').replace('xp', '');
	height = (height + 'px').replace('xp', '');
	top = (typeof (top) == 'string' || typeof (top) == 'number') ? top : 0;
	left = (typeof (left) == 'string' || typeof (left) == 'number') ? left : 0;
	title = title || '标题';
	if ($('#temp_Over_Lay1').length == 0) {
		$('<div></div>')
		.appendTo('body')
		.attr('id', 'temp_Over_Lay1')
		.css({
			'position': 'absolute',
			'left': '0px',
			'top': '0px',
			'background-color': '#000000',
			'opacity': '0.2',
			'filter': 'alpha(opacity=20)',
			'width': '100%',
			'height': '100%',
			'z-index': '11000008'
		})
		.append('<iframe src="about:blank" height="100%" width="100%"></iframe>')
		.append('<div style="position:absolute;left:0px;top:0px;width:100%;height:100%;z-index:11000009;background-color:#000000;opacity:0.2;filter:alpha(opacity=20);"></div>');
	}
	else {
		$('#temp_Over_Lay1').show();
	}

	if (width.replace('px', '') > $(document).width() - 150) {
		var xwidth = $(document).width() - 150;
		width = (xwidth < 200 ? 200 : xwidth) + 'px';
	}
	if (height.replace('px', '') > $(document).height() - 150) {
		var xheight = $(document).height() - 150;
		height = (xheight < 100 ? 100 : xheight) + 'px';
	}

	if ($('#temp_op_container1').length == 0) {
	    $('<div></div>')
		.appendTo('body')
		.html(
			'	<style type="text/css">' +
			'		.ichTop{ background:url(./images/ch_ich_top_bg1.gif) no-repeat left top; }' +
			'		.ichTopInt{height:26px;line-height:26px;background:url(./images/ch_ich_top_bg2.gif) no-repeat right top;padding:0 10px;}' +
			'		h1.ichhead{ color:#FFF; line-height:26px;font-weight:normal; font-size:12px;background:url(./images/ch_ich_top_bg3.gif) repeat-x left top;}' +
			'		h1.ichhead span{ float:right; margin-top:5px; height:15px; width:15px; cursor:pointer; background:url(./images/ch_ridi_ico.gif) no-repeat left top }' +
			'		h1.ichhead span.sp01{ background:url(./images/ch_ico_rBhelp.gif) no-repeat left 1px; margin-right:10px; line-height:20px; height:20px; width:auto; padding-left:20px;margin-top:4px;}' +
			'		.ichText{ margin-left:0; background:#FFF; padding:1px; border:2px solid #3182CE; text-align:center}' +
			'	</style>' +
			'	<div style="padding-top:0px;border: solid 1px #81ADE8; padding:1px; background:#fff;">' +
			'		<div id="tempDragHeader1" class="ichTop" style="cursor:move;">' +
			'			<div id="topdiv" class="ichTopInt">' +
			'				<h1 class="ichhead">' +
			'				<span id="spanColse1" title="关闭"></span>' +
			'				<span class="sp01" title="帮助" style="display:none;" id="divWindowHelp1">帮助</span>' +
			'				<font id="divWindowTitle1"></font>' +
			'				</h1>' +
			'			</div>' +
			'		</div>' +
			'		<div class="ichText" id="diviframeContainer1">' +
			'		<iframe id="tmp_opt_iframe1" src="' + url + '" frameborder="0" style="width:100%;height:100%;border:0px:margin:0px;padding:0px;" scroll="auto"></iframe>' +
			'		</div>' +
			'	</div>' +
			'	<span id="spandrager1" title="拖拽改变窗口大小" style="cursor:se-resize;position:absolute;bottom:0px;right:0px;z-index:110000002;"><img src="./images/bb.gif" style="width:10px;height:10px"/></span>'
			)
		.attr({ 'id': 'temp_op_container1', 'class': 'rightBox' })
		.css({
		    'left': (left ? left : (($(document).width() - width.replace('px', '')) / 2 + 'px')),
		    'top': (top ? top : '150px'),
		    'background-color': '#d0d0d0',
		    'position': 'absolute',
		    'z-index': '11000010',
		    'display': ''
		});
	    $('#spanColse1').mousedown(function() {
	        fn_del_div1();
	        goback(arguments[0]);
	    });
	    new dragResize(document.getElementById('spandrager1'), document.getElementById('diviframeContainer1'), document.getElementById('temp_Over_Lay1'));
	    new dragMove(document.getElementById('tempDragHeader1'), document.getElementById('temp_op_container1'), document.getElementById('temp_Over_Lay1'));
	}
	else {
	    $('#tmp_opt_iframe1')[0].src = url;
	    $('#temp_op_container1').css({
	        'left': (left ? left : (($(document).width() - width.replace('px', '')) / 2 + 'px')),
	        'top': (top ? top : '150px')
	    }).show();
	}
	if (typeof callback == "function") {
		$('#tmp_opt_iframe1')[0].callback = callback;
	}
	$('#divWindowTitle1').html(title);
	document.getElementById('diviframeContainer1').style.width = width;
	document.getElementById('diviframeContainer1').style.height = height;
	if (document.all) {
		document.getElementById('temp_op_container1').style.width = (parseInt(width) + 10) + 'px';
		document.getElementById('temp_op_container1').style.height = (parseInt(height) + 40) + 'px';
	}
}

function fn_add_div(url, left, top, width, height, title, callback) {
	width = (width + 'px').replace('xp', '');
	height = (height + 'px').replace('xp', '');
	top = (typeof (top) == 'string' || typeof (top) == 'number') ? top : false;
	left = (typeof (left) == 'string' || typeof (left) == 'number') ? left : false;
	title = title || '标题';
	if ($('#temp_Over_Lay').length == 0) {
		$('<div></div>')
		.appendTo('body')
		.attr('id', 'temp_Over_Lay')
		.css({
			'position': 'absolute',
			'left': '0px',
			'top': '0px',
			'background-color': '#000000',
			'opacity': '0.2',
			'filter': 'alpha(opacity=20)',
			'width': '100%',
			'height': '100%',
			'z-index': '9999998'
		})
		.append('<iframe src="about:blank" height="100%" width="100%" frameborder="0"></iframe>')
		.append('<div style="position:absolute;left:0px;top:0px;width:100%;height:100%;z-index:9999999;"></div>');
	}
	else {
		$('#temp_Over_Lay').show();
	}

	if (width.replace('px', '') > $(document).width() - 150) {
		var xwidth = $(document).width() - 150;
		width = (xwidth < 200 ? 200 : xwidth) + 'px';
	}
	if (height.replace('px', '') > $(document).height() - 150) {
		var xheight = $(document).height() - 150;
		height = (xheight < 100 ? 100 : xheight) + 'px';
	}


	if ($('#temp_op_container').length == 0) {
	    $('<div></div>')
		.appendTo('body')
		.html(
			'	<style type="text/css">' +
			'		.ichTop{ background:url(./images/ch_ich_top_bg1.gif) no-repeat left top; }' +
			'		.ichTopInt{height:26px;line-height:26px;background:url(./images/ch_ich_top_bg2.gif) no-repeat right top;padding:0 10px;}' +
			'		h1.ichhead{ color:#FFF; line-height:26px;font-weight:normal; font-size:12px;background:url(./images/ch_ich_top_bg3.gif) repeat-x left top;}' +
			'		h1.ichhead span{ float:right; margin-top:5px; height:15px; width:15px; cursor:pointer; background:url(./images/ch_ridi_ico.gif) no-repeat left top }' +
			'		h1.ichhead span.sp01{ background:url(./images/ch_ico_rBhelp.gif) no-repeat left 1px; margin-right:10px; line-height:20px; height:20px; width:auto; padding-left:20px;margin-top:4px;}' +
			'		.ichText{ margin-left:0; background:#FFF; padding:1px; border:2px solid #3182CE; text-align:center}' +
			'	</style>' +
			'	<div style="padding-top:0px;border: solid 1px #81ADE8; padding:1px; background:#fff;">' +
			'		<div id="tempDragHeader" class="ichTop" style="cursor:move;">' +
			'			<div id="topdiv" class="ichTopInt">' +
			'				<h1 class="ichhead">' +
			'				<span id="spanColse" title="关闭"></span>' +
			'				<span class="sp01" title="帮助" style="display:none;" id="divWindowHelp">帮助</span>' +
			'				<font id="divWindowTitle"></font>' +
			'				</h1>' +
			'			</div>' +
			'		</div>' +
			'		<div class="ichText" id="diviframeContainer">' +
			'		<iframe id="tmp_opt_iframe" src="' + url + '" frameborder="0" style="width:100%;height:100%;border:0px:margin:0px;padding:0px;" scroll="auto"></iframe>' +
			'		</div>' +
			'	</div>' +
			'	<span id="spandrager" title="拖拽改变窗口大小" style="cursor:se-resize;position:absolute;bottom:0px;right:0px;z-index:10000002;"><img src="./images/bb.gif" style="width:10px;height:10px"/></span>'
			)
		.attr({ 'id': 'temp_op_container', 'class': 'rightBox' })
		.css({
			'left': (left ? left : (($(document).width() - width.replace('px', '')) / 2 + 'px')),
			'top': (top ? top : '150px'),
			'background-color': '#d0d0d0',
			'position': 'absolute',
			'z-index': '10000000',
			'display': 'none'
		}).fadeIn(600);
		$('#spanColse').mousedown(function() { fn_del_div(); goback(arguments[0]); });
		new dragResize(document.getElementById('spandrager'), document.getElementById('diviframeContainer'), document.getElementById('temp_Over_Lay'));
		new dragMove(document.getElementById('tempDragHeader'), document.getElementById('temp_op_container'), document.getElementById('temp_Over_Lay'));
	}
	else {
		$('#tmp_opt_iframe')[0].src = url;
		$('#temp_op_container').css({
			'left': (left ? left : (($(document).width() - width.replace('px', '')) / 2 + 'px')),
			'top': (top ? top : '150px')
		}).fadeIn(600);
	}
	if (typeof callback == "function") {
		$('#tmp_opt_iframe')[0].callback = callback;
	}
	$('#divWindowTitle').html(title);

	document.getElementById('diviframeContainer').style.width = width;
	document.getElementById('diviframeContainer').style.height = height;
	if (document.all) {
	    document.getElementById('temp_op_container').style.width = (parseInt(width) + 10) + 'px';
	    document.getElementById('temp_op_container').style.height = (parseInt(height) + 40) + 'px';
	}
}




function goback() {
	if (document.all) {
		evt = window.event;
		evt.cancelBubble = true;
	} else {
		evt = arguments[0];
		evt.stopPropagation();
	}
}



//修改弹出窗口的边距、宽高等
//示例：top.fn_mod_div('100px','100px','600px','500px');
//		top.fn_mod_div('','','600px','500px');
//说明：如果左边距和右边距不指定则自动居中
function fn_mod_div(left, top, width, height) {
	if (width.replace('px', '') > $(document).width()) {
		var xwidth = $(document).width() - 150;
		width = (xwidth < 200 ? 200 : xwidth) + 'px';
	}
	if (height.replace('px', '') > $(document).height()) {
		var xheight = $(document).height() - 150;
		height = (xheight < 100 ? 100 : xheight) + 'px';
	}
	var objDiv = window.top.document.getElementById('temp_op_container');
	objDiv.style.left = left ? left : ((window.top.document.body.scrollWidth - width.replace('px', '')) / 2 + 'px');
	objDiv.style.top = top ? top : ((window.top.document.body.scrollHeight - height.replace('px', '')) / 2 + 'px');
	objDiv.style.width = width;
	objDiv.style.height = height;
}

function fn_alert() {
	if ($('#temp_Over_Lay_alert').length == 0) {
		$('<div></div>')
		.appendTo('body')
		.attr('id', 'temp_Over_Lay_alert')
		.css({
			'position': 'absolute',
			'left': '0px',
			'top': '0px',
			'background-color': '#000000',
			'opacity': '0.2',
			'filter': 'alpha(opacity=20)',
			'width': '100%',
			'height': '100%',
			'z-index': '12000010'
		})
		.append('<iframe src="about:blank" height="100%" width="100%"></iframe>')
		.append('<div style="position:absolute;left:0px;top:0px;width:100%;height:100%;z-index:12000011;background-color:#000000;opacity:0.2;filter:alpha(opacity=20);"></div>');
	}
	else {
		$('#temp_Over_Lay_alert').show();
	}

	var strInfo = arguments[0] || '';
	if ($('#temp_op_container_alert').length == 0) {
		$('<div></div>')
		.appendTo('body')
		.html(
			'	<style type="text/css">' +
			'		.ichTop{ background:url(./images/ch_ich_top_bg1.gif) no-repeat left top; }' +
			'		.ichTopInt{height:26px;line-height:26px;background:url(./images/ch_ich_top_bg2.gif) no-repeat right top;padding:0 10px;}' +
			'		h1.ichhead{ color:#FFF; line-height:26px;font-weight:normal; font-size:12px;background:url(./images/ch_ich_top_bg3.gif) repeat-x left top;}' +
			'		h1.ichhead span{ float:right; margin-top:5px; height:15px; width:15px; cursor:pointer; background:url(./images/ch_ridi_ico.gif) no-repeat left top }' +
			'		h1.ichhead span.sp01{ background:url(./images/ch_ico_rBhelp.gif) no-repeat left 1px; margin-right:10px; line-height:20px; height:20px; width:auto; padding-left:20px;margin-top:4px;}' +
			'		.ichText{ margin-left:0; background:#FFF; padding:1px; border:2px solid #3182CE; text-align:center}' +
			'	</style>' +
			'	<div style="padding-top:0px;border: solid 1px #81ADE8; padding:1px; background:#fff;">' +
			'		<div id="tempDragHeaderalert" class="ichTop" style="cursor:move;">' +
			'			<div class="ichTopInt">' +
			'				<h1 class="ichhead">' +
			'				<span id="spanColsealert" title="关闭"></span>' +
			'				<font id="__alertTitle">消息提示</font>' +
			'				</h1>' +
			'			</div>' +
			'		</div>' +
			'		<div id="divinfoalert" style="margin:10px;word-wrap: break-word; word-break: break-all;">' + (strInfo + '').replace('\n', '<br/>') + '</div>' +
			'		<div style="margin:auto;margin-top:10px;margin-bottom:8px;text-align:center;"><input type="image" alt="确认" id="btnclosealert" src="./images/ch_wrong_ico1.gif" style="border:0px;cursor:pointer;margin:auto;" /></div>' +
			'	</div>')
		.attr({ 'id': 'temp_op_container_alert', 'class': 'rightBox' })
		.css({
			'width': '300px',
			'left': (($(document).width() - 400) / 2 + 'px'),
			'top': '200px',
			'background-color': '#d0d0d0',
			'position': 'absolute',
			'z-index': '12000012',
			'display': ''
		});
		$('#btnclosealert').click(function() {
			$('#temp_op_container_alert').hide();
			$('#temp_Over_Lay_alert').hide();
		});
		$('#spanColsealert').mousedown(function() {
			$('#temp_op_container_alert').hide();
			$('#temp_Over_Lay_alert').hide();
			goback(arguments[0]);
		});
		new dragMove(document.getElementById('tempDragHeaderalert'), document.getElementById('temp_op_container_alert'), document.getElementById('temp_Over_Lay_alert'));
	} else {
		$('#temp_op_container_alert').show();
		$('#divinfoalert').html((strInfo + '').replace('\n', '<br/>'));
		$('#__alertTitle').html('消息提示');
		$('#temp_op_container_alert').width(300);
	}
	$('#btnclosealert').focus();
}

function fn_onload() {
	var strInfo = (arguments.length > 0) ? arguments[0] : '<span style="color:#ff0000;background-color:#FFFFFF;">数据加载中，请稍候……</span>';
	if ($('#temp_Over_Lay_loading').length == 0) {
		$('<div><div style="margin-top:' + parseInt(($(document).height() - 30) / 2) + 'px;text-align:center;">' + strInfo + '</div></div>')
		.appendTo('body')
		.attr('id', 'temp_Over_Lay_loading')
		.css({
			'position': 'absolute',
			'left': '0px',
			'top': '0px',
			'background-color': '#FEFEFE',
			'opacity': '0.7',
			'filter': 'alpha(opacity=70)',
			'width': '100%',
			'height': '100%',
			'z-index': '950000000'
		});
	}
	else {
		$("#temp_Over_Lay_loading div:first-child").html(strInfo);
		$('#temp_Over_Lay_loading').show();
	}
	setTimeout(function() { $('#temp_Over_Lay_loading').hide(); }, 12000);
	return function() {
		$('#temp_Over_Lay_loading').hide();
	}
}

function showMask(strInfo) {
	if (arguments.length > 0) {
		fn_onload(strInfo);
	}
	else {
		fn_onload();
	}
}
function hideMask() {
	try {
		$('#temp_Over_Lay_loading').hide();
	} catch (e) { }
}

//var Alert = window.alert;
//window.alert = function() { 
//	if (arguments.length == 2 && arguments[1]) { Alert(arguments[0]); return; }
//    fn_alert(arguments[0]);  
//}



var dragResize = function(obj, ptobj, objOverlay) {
    var _xMove = 0, _yMove = 0;
    var _this = this;
    var pobj = null;
    var construct = function() {
        pobj = obj.parentNode;
        obj.onmousedown = start;
    }
    var start = function(event) {
        event = fixEvent(event);
        if (event.which != 1) {
            return true;
        }
        _xMove = event.clientX;
        _yMove = event.clientY;
        showCoverDiv();
        document.onmouseup = end;
        document.onmousemove = move;
        document.onselectstart = function() { return false; }
        return false;
    }
    var move = function(event) {
        event = fixEvent(event);
        if (event.which == 0) {
            return end;
        }
        var _clientX = event.clientX || 0;
        var _clientY = event.clientY || 0;
        if (_xMove == _clientX && _yMove == _clientY) {
            return false;
        }
        if ((parseInt(ptobj.style.width) + _clientX - _xMove) < 200 || (parseInt(ptobj.style.height) + _clientY - _yMove) < 100) {
            return false;
        }
        ptobj.style.width = (parseInt(ptobj.style.width) + _clientX - _xMove) + 'px';
        ptobj.style.height = (parseInt(ptobj.style.height) + _clientY - _yMove) + 'px';
        if (document.all) {
            pobj.style.width = (parseInt(pobj.style.width) + _clientX - _xMove) + 'px';
            pobj.style.height = (parseInt(pobj.style.height) + _clientY - _yMove) + 'px';
        }
        _xMove = _clientX;
        _yMove = _clientY;
        return false;
    }
    var end = function(event) {
        hideCoverDiv();
        event = fixEvent(event);
        document.onmousemove = null;
        document.onmouseup = null;
        if (navigator.userAgent.indexOf("Opera") != -1) {
            document.body.style.display = "none";
            document.body.style.display = "";
        }
        document.onselectstart = function() { return true; }
        return true;
    }
    var fixEvent = function(ig_) {
        if (typeof ig_ == "undefined") {
            ig_ = window.event;
        }
        if (typeof ig_.layerX == "undefined") {
            ig_.layerX = ig_.offsetX;
        }
        if (typeof ig_.layerY == "undefined") {
            ig_.layerY = ig_.offsetY;
        }
        if (typeof ig_.which == "undefined") {
            ig_.which = ig_.button;
        }
        return ig_;
    }
    var showCoverDiv = function() {
        if (objOverlay) {
            objOverlay.style.zIndex = parseInt(objOverlay.style.zIndex) + 200;
        }
    }
    var hideCoverDiv = function() {
        if (objOverlay) {
            objOverlay.style.zIndex = parseInt(objOverlay.style.zIndex) - 200;
        }
    }
    construct();
}


var dragMove = function(objheader, objContainer, objOverlay) {
	var offH = 0;
	var _this = this;
	var construct = function() {
		objContainer = objContainer || objheader.parentNode;
		offH = objContainer.style.top;
		objheader.onmousedown = ts_start;
		document.onscroll = ts_scroll;
	}
	var ts_start = function(event) {
		event = fixEvent(event);
		if (event.which != 1) {
			return true;
		}
		showCoverDiv();
		objContainer.lastMouseX = event.clientX;
		objContainer.lastMouseY = event.clientY;
		document.onmouseup = ts_end;
		document.onmousemove = ts_move;
		document.onselectstart = function() { return false; }
		return false;
	}
	var ts_move = function(event) {
		event = fixEvent(event);
		if (event.which == 0) {
			return ts_end;
		}
		var _clientX = event.clientY;
		var _clientY = event.clientX;
		if (objContainer.lastMouseX == _clientY && objContainer.lastMouseY == _clientX) {
			return false;
		}
		var _lastX = parseInt(objContainer.style.top);
		var _lastY = parseInt(objContainer.style.left);
		var newX, newY;
		newX = _lastY + _clientY - objContainer.lastMouseX;
		newY = _lastX + _clientX - objContainer.lastMouseY;
		objContainer.style.left = newX + "px";
		objContainer.style.top = newY + "px";
		objContainer.lastMouseX = _clientY;
		objContainer.lastMouseY = _clientX;
		return false;
	}

	var ts_end = function(event) {
		hideCoverDiv();
		event = fixEvent(event);
		offH = parseInt(objContainer.style.top.replace(/px/gi, '')) - getScrollTop();
		document.onmousemove = null;
		document.onmouseup = null;
		if (navigator.userAgent.indexOf("Opera") != -1) {
			document.body.style.display = "none";
			document.body.style.display = "";
		}
		document.onselectstart = null;
		return true;
	}
	var ts_scroll = function() {
		objContainer.style.top = (getScrollTop() + parseInt(offH)) + "px";
	}
	var getScrollTop = function() {
		var theTop;
		if (document.documentElement && document.documentElement.scrollTop)
			theTop = document.documentElement.scrollTop;
		else if (document.body)
			theTop = document.body.scrollTop;
		return theTop;
	}
	var fixEvent = function(ig_) {
		if (typeof ig_ == "undefined") {
			ig_ = window.event;
		}
		if (typeof ig_.layerX == "undefined") {
			ig_.layerX = ig_.offsetX;
		}
		if (typeof ig_.layerY == "undefined") {
			ig_.layerY = ig_.offsetY;
		}
		if (typeof ig_.which == "undefined") {
			ig_.which = ig_.button;
		}
		return ig_;
	}
	var showCoverDiv = function() {
		if (objOverlay) {
			objOverlay.style.zIndex = parseInt(objOverlay.style.zIndex) + 200;
		}
	}
	var hideCoverDiv = function() {
		if (objOverlay) {
			objOverlay.style.zIndex = parseInt(objOverlay.style.zIndex) - 200;
		}
	}
	construct();
}
