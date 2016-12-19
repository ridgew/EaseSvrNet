//页面加载执行
$(function()
{
	var _id = top.getPageParamVal(this,"id");
	if (!isNaN(_id) && _id > 0)
	{
		var _Ajax = new top.AJAXFunction({url:"./service/1.3.6.4.4",data:"{SOFTWARE_ID:" + _id + "}"});
		_Ajax.SuFun = function(data)
		{
			data = data.d.Data;
			for (var json in data)
			{
				if (json != "USER_SEX")
					$("#" + json).html(data[json]);
				else
					$("#" + json).html(data[json] == 0 ? "男" : "女");
			}
		};
		_Ajax.SendFun();
	}
	else
	{
		alert("页面参数传递错误！");
		window.top.fn_del_div();
		return;
	}

	//绑定页面按钮事件
	$("#btn_Close").click(function(){window.top.fn_del_div();}).attr({style:"cursor:pointer;"});
});
