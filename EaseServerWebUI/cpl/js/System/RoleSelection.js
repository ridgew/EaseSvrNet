/// <reference path="../jquery.js" />
$(document).ready(function() {
    var op = $.QueryString().op;
    if (op == "1") {
        var userIds = eval('[' + $.QueryString().ids + ']');
        $('#aSave').click(function() {
            var roleNames = [];
            $('#roleList input:checkbox[checked=true]').each(function(i) {
                roleNames[i] = $(this).val();
            });
            if (userIds.length > 0 && roleNames.length > 0) {
                var addAjax = new top.AJAXFunction({ url: './service/1.3.5.2.7', param: { userIds: userIds, roleNames: roleNames} });
                addAjax.SuFun = function(json) {
                    if (json.d.Status == 10034) {
                        top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
                    }
                };
                addAjax.SendFun();
            }
            else
                alert('请先选择要添加的系统角色');
        });
        var roleAjax = new top.AJAXFunction({ url: './service/1.3.5.2.1' });
        roleAjax.SuFun = function(json) {
            var roleList = json.d.Data;
            $(roleList).each(function(i) {
                $('#roleList').append('<input value="' + this.RoleName + '" id="chkbxRole_' + i + '" name="chkbxRole$' + i + '" type="checkbox"><label for="chkbxRole_' + i + '">' + this.Description + '</label>&nbsp;');
            });
        };
        roleAjax.SendFun();
    }
    else if (op == "2") {
        var userIds = eval('[' + $.QueryString().ids + ']');
        $('#aSave').click(function() {
            var roleNames = [];
            $('#roleList input:checkbox[checked=true]').each(function(i) {
                roleNames[i] = $(this).val();
            });
            if (userIds.length > 0 && roleNames.length > 0) {
                var delAjax = new top.AJAXFunction({ url: './service/1.3.5.2.9', param: { userIds: userIds, roleNames: roleNames} });
                delAjax.SuFun = function(json) {
                    if (json.d.Status == 10035) {
                        top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
                    }
                };
                delAjax.SendFun();
            }
            else
                alert('请先选择要移除的系统角色');
        });
        var roleAjax = new top.AJAXFunction({ url: './service/1.3.5.2.1' });
        roleAjax.SuFun = function(json) {
            var roleList = json.d.Data;
            $(roleList).each(function(i) {
                $('#roleList').append('<input value="' + this.RoleName + '" id="chkbxRole_' + i + '" name="chkbxRole$' + i + '" type="checkbox"><label for="chkbxRole_' + i + '">' + this.Description + '</label>&nbsp;');
            });
        };
        roleAjax.SendFun();
    }
});