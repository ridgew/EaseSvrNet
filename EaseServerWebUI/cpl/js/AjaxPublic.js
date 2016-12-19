//JavaScript Document
//简易公用AJAX请求
function AJAXFunction() {
	var _this = this;
	//请求参数
	_this.showMaskTF = typeof(arguments[1])=='boolean' ? arguments[1] : false;
	this.config = arguments[0] || {};
	this.type = this.config.type || "POST";
	this.url = this.config.url || "";
	this.contentType = this.config.contentType || "application/json; charset=utf-8";
	this.dataType = this.config.dataType || "json";
	this.async = typeof(this.config.async)=='boolean' ? this.config.async : true;
	this.param = this.config.param || {};
	this.data = this.config.data || $.JSON.encode(this.param);	
	
	//请求发送
	this.SendFun = function() {
		if (_this.showMaskTF) top.showMask();
		$.ajax({
			type: this.type,
			url: this.url,
			contentType: this.contentType,
			dataType: this.dataType,
			data: this.data,
			async: this.async,
			complete: function(xmlObj, Status) { if (_this.showMaskTF)top.hideMask(); _this.CpFun(xmlObj, Status); },
			error: function(xmlObj, Status) { if (Status == 'parsererror') { top.window.location.href = top.window.location.href.replace('index.html', 'login.html'); return; } if (typeof (_this.FaFun) != "function") { alert('网络或者程序异常'); } else { _this.FaFun(xmlObj, Status); } },
			success: function(data) {
				if (data.d.Status >= 1) {
					if (data.d.Status > 10000)
						alert(data.d.Message);
					_this.SuFun(data);
				}
				else {
					alert(data.d.Message);
				}
			}
		});
	};
	//请求失败和成功
	this.SuFun = function() { };
	this.FaFun = null;
	this.CpFun = function() { };
}