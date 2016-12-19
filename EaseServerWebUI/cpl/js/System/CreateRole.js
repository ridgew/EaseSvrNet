/// <reference path="../jquery.js" />
$(document).ready(function() {
    $('#aReset').click(function() {
        $('.listIB input.inp02').val('');
    });
    $('#aSave').click(function() {
        var roleName = $('#txtRoleName').val();
        var roleDescription = $('#txtRoleDescription').val();
        if (roleName.length == 0)
            alert('请输入系统角色名称');
        else {
            var saveAjax = new top.AJAXFunction({ url: './service/1.3.5.2.2', param: { roleName: roleName, roleDescription: roleDescription} });
            saveAjax.SuFun = function(json) {
                if (json.d.Status == 10028) {
                    top.deskTabs.getFrameLeft().contentWindow.location.href = './System/RoleTree.html';
                }
            };
            saveAjax.SendFun();
        }
    });
});