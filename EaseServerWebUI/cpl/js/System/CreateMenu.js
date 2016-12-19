/// <reference path="../jquery.js" />
$(document).ready(function() {
    var menuContent;
    var parentId = jQuery.QueryString().ParentId;

    $('#aReset').click(function() {
        $('.listIB input.inp02').val('');
        $('#roleList input:checkbox').attr('checked', '');
    });
    $('#aSave').click(function() {
        var keyCode = $('#trKeyCode').css('display') == 'none' ? 0 : $('#dplstKeyCode').find('option:selected').text().charCodeAt(0);
        var roleIdList = [];
        $('#roleList input:checkbox[checked=true]').each(function(i) {
            roleIdList[i] = $(this).val();
        });
        var createAjax = new top.AJAXFunction({ url: './service/1.3.5.1.7', data: jQuery.JSON.encode({ parentId: parentId, menuName: $('#txtName').val(), leftUrl: $('#txtLeftUrl').val(), rightUrl: $('#txtRightUrl').val(), keyCode: keyCode, enabled: $('input:radio[name="rdbtnEnabled"][checked=true]').val() == 1 ? true : false, roleIdList: roleIdList }) });
        createAjax.SuFun = function(json) {
            if (json.d.Status = 10005) {
                if (jQuery.QueryString().Tab == 'true')
                    top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
            }
        };
        top.showMask();
        createAjax.SendFun();
    });

    if (parentId == 0) {
        $('#menuPath').html('<a style="color:Tomato">顶级菜单</a>');
        $('#rdbtnEnabled_0').attr('checked', 'checked');
        $('#trKeyCode').css('display', '');
        $('#dplstKeyCode').change(function() {
            var key = $(this).find('option:selected').text();
            var reg = new RegExp('\\(' + key + '\\)$');
            var name = $('#txtName').val();
            if (name.lastIndexOf(")") + 1 == name.length) {
                if (!reg.test($('#txtName').val()))
                    $('#txtName').val(name.replace(/(.*\()([A-Z])(\))$/g, '$1' + key + '$3'));
            }
            else {
                $('#txtName').val(name + "(" + key + ")");
            }
        });

        var roleAjax = new top.AJAXFunction({ url: './service/1.3.5.2.1' });
        roleAjax.SuFun = function(json) {
            var roleList = json.d.Data;
            $(roleList).each(function(i) {
                $('#roleList').append('<input value="' + this.RoleId + '" id="chkbxRole_' + i + '" name="chkbxRole$' + i + '" type="checkbox"><label for="chkbxRole_' + i + '">' + this.Description + '</label>&nbsp;');
            });
        };
        roleAjax.SendFun();
    }
    else if (parentId > 0) {
        var roleAjax = new top.AJAXFunction({ url: './service/1.3.5.2.1' });
        roleAjax.SuFun = function(json) {
            var roleList = json.d.Data;
            $(roleList).each(function(i) {
                $('#roleList').append('<input value="' + this.RoleId + '" id="chkbxRole_' + i + '" name="chkbxRole$' + i + '" type="checkbox"><label for="chkbxRole_' + i + '">' + this.Description + '</label>&nbsp;');
            });
            loadMenuInfo();
        };
        roleAjax.SendFun();
    }

    function loadMenuInfo() {
        var menuAjax = new top.AJAXFunction({ url: './service/1.3.5.1.4', data: '{menuId:' + parentId + '}' });
        menuAjax.SuFun = function(json) {
            menuContent = json.d.Data;
            if (menuContent.Exists) {
                $('#menuPath').html(menuContent.MenuPath);
                $('#rdbtnEnabled_' + (menuContent.Enabled ? '0' : '1')).attr('checked', 'checked');
                $(menuContent.Roles).each(function(i) {
                    $('input:checkbox[value="' + this.RoleId + '"]').attr('checked', 'checked');
                });
            }
        };
        menuAjax.SendFun();
    }
});