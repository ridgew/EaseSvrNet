/// <reference path="../jquery.js" />
$(document).ready(function() {
    $('#aReset').click(function() {
        $('.listIB input.inp02').val('');
        $('#txtDescription').val('');
    });
    $('#aSave').click(function() {
        var name = $('#txtName').val();
        var value = $('#txtValue').val();
        var description = $('#txtDescription').val();
        if (name.length == 0)
            alert('请输入系统参数名称');
        else {
            var saveAjax = new top.AJAXFunction({ url: './service/1.3.5.5.2', param: { name: name, value: value, description: description} });
            saveAjax.SuFun = function(json) {
                if (json.d.Status == 10014) {
                    top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
                    top.fn_del_div();
                }
            };
            saveAjax.SendFun();
        }
    });
    var name = $.QueryString().name;
    if (name && name.length > 0) {
        var getAjax = new top.AJAXFunction({ url: './service/1.3.5.5.3', param: { name: name} });
        getAjax.SuFun = function(json) {
            $('#txtName').val(json.d.Data.Name);
            $('#txtValue').val(json.d.Data.Value);
            $('#txtDescription').val(json.d.Data.Description);
        };
        getAjax.SendFun();
    }
});