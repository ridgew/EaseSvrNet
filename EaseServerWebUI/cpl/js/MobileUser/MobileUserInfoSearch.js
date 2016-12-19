//手机用户搜索JS部分
$(function()
{
	//绑定页面按钮事件
	$("#btn_Save").click(function(){Submit_Search();}).attr({style:"cursor:pointer;"});
	$("#btn_Close").click(function(){window.top.fn_del_div();}).attr({style:"cursor:pointer;"});
});

//提交搜索
function Submit_Search()
{
	var SearchField = ["SOFTWARE_ID"];
	var SearchCondition = [4];
	var SearchValue = [0];

	var _TxtFieldArr = ["USER_NAME", "MSID", "USER_ID_CARD", "USER_ADDR", "IMSI", "IMEI", "REMOTE_IP", "UserAgent"];
	var _SelFieldArr = ["USER_SEX","USER_AGE"];

	for (var t = 0; t < _TxtFieldArr.length; t ++)
	{
		if ($.trim($("#" + _TxtFieldArr[t]).val()) != "")
		{
			SearchField[SearchField.length] = _TxtFieldArr[t];
			SearchCondition[SearchCondition.length] = 6;
			SearchValue[SearchValue.length] = $.trim($("#" + _TxtFieldArr[t]).val());
		}
	}

	for (var s = 0; s < _SelFieldArr.length; s ++)
	{
		if ($.trim($("#" + _SelFieldArr[s]).val().replace(/[^0-9]*/gi, '')) != "")
		{
			SearchField[SearchField.length] = _SelFieldArr[s];
			SearchCondition[SearchCondition.length] = 0;
			SearchValue[SearchValue.length] = $.trim($("#" + _SelFieldArr[s]).val().replace(/[^0-9]*/gi, ''));
		}
	}

	var _WinObj = window.top.deskTabs.getFrameRight().contentWindow;
	_WinObj.pageConfig.SearchField = SearchField;
	_WinObj.pageConfig.SearchCondition = SearchCondition;
	_WinObj.pageConfig.SearchValue = SearchValue;
	_WinObj.Init_PageDataListConfig();
	window.top.fn_del_div();
}