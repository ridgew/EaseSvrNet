$(function() {
	document.getElementById('btnToldClose').onclick = function() {
		document.getElementById('pnlToldPanel').style.display = 'none';
		document.getElementById('pnlToldPanelMask').style.display = 'none';
	};
	document.getElementById('btnToldHistory').onclick = function() {
		deskTabs.iframeOpen('消息查看', '/WIMP/sysmanage/sysMeAmend.html');
		document.getElementById('pnlToldPanel').style.display = 'none';
		document.getElementById('pnlToldPanelMask').style.display = 'none';
	};
	document.getElementById('btnShowInformation').onclick = function() {
		if (document.getElementById('pnlToldPanel').style.display == '') {
			document.getElementById('pnlToldPanel').style.display = 'none';
			document.getElementById('pnlToldPanelMask').style.display = 'none';
		} else {
			document.getElementById('pnlToldPanel').style.display = '';
			document.getElementById('pnlToldPanelMask').style.display = '';
			//__getInformations('./SystemServlet?method=getUnReadedInformation', GetUnReadedInformation);
		}
	};
	document.getElementById('pnlToldBody').style.wordBreak = 'break-all';
	//__getInformations('./SystemServlet?method=getUnReadedInformation', GetUnReadedInformation);
	//__getInformations('./SystemServlet?method=getOperatorInfo', GetOperatorInfo);
	//var tody = new Date();
	//document.getElementById('__btnDATE').innerHTML = tody.getFullYear() + '-' + (tody.getMonth() + 1) + '-' + tody.getDate();
	//__btnFREE_TIME,__btnDATE
});

var GetUnReadedInformation = function(data) {
	if (data.info) {
		document.getElementById('pnlToldPanel').style.display = '';
		document.getElementById('pnlToldPanelMask').style.display = '';
		var title = data.info[0].NAME.substr(0, 20);
		if (data.info[0].NAME.length > 20) {
			title += '..';
		}
		document.getElementById('lblToldTitle').title = data.info[0].NAME;
		document.getElementById('lblToldTitle').innerHTML = title;
		document.getElementById('pnlToldBody').innerHTML = '<p>' + data.info[0].NOTE + '</p>';
	} else {
		document.getElementById('lblToldTitle').innerHTML = '没有最新消息';
		document.getElementById('pnlToldBody').innerHTML = '';
	}
};

var GetOperatorInfo = function(data) {
	if (data) {
		for (var field in data) {
			try {
				document.getElementById('__btn' + field).innerHTML = data[field];
			} catch (ex) { }
		}
	}
};
var __getInformations = function(url, callback) {
	$("#pnlToldPanel").block({ message: '数据加载中...', css: { width: "30%", left: "70%", top: "10%"} });
	$.ajax({
		type: 'GET',
		url: url,
		dataType: 'json',
		cache: false,
		complete: function() { $("#pnlToldPanel").unblock(); },
		success: function(data) {
			callback(data);
		}
	});
};