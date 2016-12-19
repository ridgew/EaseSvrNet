jQuery(function($) {
	window.HelpInfo = { HelpTitle: '', HelpContent: '' };
	var querystr = $.QueryString();
	var ajaxObj;
	if (querystr.MenuId) {
		ajaxObj = new top.AJAXFunction({ url: './service/1.3.6.1.2', param: { MenuId: querystr.MenuId} });
		ajaxObj.SuFun = function(data, status) {
			var data = data.d.Data;
			$('#pnlHelpContent').html(data.HelpContent);
			if (data['MenuId'] > 0) {
				window.HelpInfo = data;
			} else {
				switchMode();
			}
		};
		ajaxObj.SendFun();
	}
	$("#btnSubmit").click(function(e) {
		e.preventDefault();
		var params = {};
		var url = './service/1.3.6.1.1';
		params.HelpTitle = "";
		var _editor = FCKeditorAPI.GetInstance('HelpContent');
		params.HelpContent = $.trim(_editor.GetXHTML(true));
		params.MenuId = querystr.MenuId;
		var errInfo = '';

		if (params.HelpContent.length < 1) {
			errInfo += '内容不能为空。\n';
		}
		if (errInfo.length > 0) {
			alert(errInfo);
		} else {
			var ajaxObj = new top.AJAXFunction({ url: url, param: params });
			ajaxObj.SuFun = function(data, status) {
				alert(data.d.Message);
				try {
					location.reload();
				} catch (ex) {
				}
			};
			ajaxObj.SendFun();
		}
	});
	$('#btnEdit').click(function(e) {
		e.preventDefault();
		switchMode();
	}).css('cursor', 'pointer');
	$("#btnReset").click(function(e) {
		e.preventDefault();
		var _editor = FCKeditorAPI.GetInstance('HelpContent');
		_editor.SetData(window.HelpInfo['HelpContent']);
	});
	$("#btnCancel").click(function(e) {
		$('#op_div').hide();
		$('#pnlHelpContent').html(window.HelpInfo['HelpContent']);
	});
});

function switchMode() {
	var _editor = new FCKeditor('HelpContent');
	//_editor.BasePath = sBasePath;
	_editor.Height = 400;
	_editor.Value = window.HelpInfo['HelpContent'];
	$('#pnlHelpContent').html(_editor.CreateHtml());
	$('#op_div').show();
}