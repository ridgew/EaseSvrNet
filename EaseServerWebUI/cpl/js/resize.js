/*********************************
$Header: /Gwsoft-Ease/admin/js/resize.js 1     09-05-19 9:36 Baiye $
Ridge Wong @ 2008年8月26日
-----------
//@require('RenderTabs.js')
********************************/
function setPageResize() {
	var _height, _prehnum;
	try {
		_height = document.documentElement.clientHeight;
	} catch (e) {
		_height = 768;
	}
	var _bn = navigator.userAgent.toLowerCase();
	if (_bn.indexOf("msie 6.0") != -1) {
		_prehnum = 158;
	} else if (_bn.indexOf("msie 7.0") != -1) {
		_prehnum = 150;
	} else if (_bn.indexOf("firefox") != -1) {
		_prehnum = 150;
	} else {
		_prehnum = 155;
	}
	document.getElementById("content").style.height = (_height - _prehnum) + "px";
	document.getElementById("c_Tree").style.height = (_height - _prehnum - 52) + "px";
	document.getElementById("s_cut").style.height = (_height - _prehnum - 52) + "px";

	//更新右侧容器的宽和高 !important
	$("#right").css("height", (_height - _prehnum) + "px");
	//console.log($("#right").css("margin-left"))
	var iLeftWidth = $("#left").width();
	if ("none" == $("#left").css("display").toLowerCase()) iLeftWidth = 0;
	$("#right").css("width", ($("#content").width() - iLeftWidth - $("#line").width() - 12) + "px");
	var _line = document.getElementById("line_ico");
	_line.style.marginTop = parseInt((_height - _prehnum - 35) / 2) + "px";

//	var _tmpFrameDiv = document.getElementById("FrameDiv");
//	_tmpFrameDiv.style.height = (_height - _prehnum - 27) + "px";
//	var _tmpFrameArr = _tmpFrameDiv.getElementsByTagName("iframe");
//	for (var i = 0; i < _tmpFrameArr.length; i++) {
//		_tmpFrameArr[i].style.height = (_height - _prehnum - 27) + "px";
//	}

	if (typeof deskTabs != "undefined") deskTabs.resizeTo();
	window.onresize = function() { setPageResize(); };
	window.onscroll = function() { setPageResize(); };
}