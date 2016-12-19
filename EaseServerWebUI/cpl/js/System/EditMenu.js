/// <reference path="../jquery.js" />
var __grid;
$(document).ready(
function() {
    var menuId = jQuery.QueryString().MenuId;
    //$("#DelShortcut").css('display','none');
    //$("#AddShortcut").css('display','none');
    var fmenuId = jQuery.QueryString().FMenuId;
    if(fmenuId&&fmenuId!="undefined"&&fmenuId!=0)
    {
        var delAjax = new top.AJAXFunction({ url: './service/1.3.5.1.12', param: { menuId: menuId} });
            delAjax.SuFun = function(json) {
                if(json.d.IsShortcut)
                {
                    $("#AddShortcut").css('display','block');
                }
                else
                {
                    $("#DelShortcut").css('display','block');
                }
            };
        delAjax.SendFun();
    }
    
    if (menuId == 0) {
        $('#menuPath').html('<a style="color:Tomato">顶级菜单</a>');
        $('#row3').css('display', 'none');
        $('#row4').css('display', 'none');
        $('#dvTop').css('display', 'none');
    }
    
    $("#AddShortcut").click(function(){
        var delAjax = new top.AJAXFunction({ url: './service/1.3.5.1.13', param: { menuId: menuId} });
            delAjax.SuFun = function(json) {
                alert("添加成功");
                $("#AddShortcut").css('display','none');
                $("#DelShortcut").css('display','block');
            };
        delAjax.SendFun();
    });
    $("#DelShortcut").click(function(){
        var delAjax = new top.AJAXFunction({ url: './service/1.3.5.1.14', param: { menuId: menuId} });
            delAjax.SuFun = function(json) {
                alert("删除成功");
                $("#AddShortcut").css('display','block');
                $("#DelShortcut").css('display','none');
            };
        delAjax.SendFun();
    });
    
    $('#aCreate').click(function() {
        top.fn_add_div('./System/CreateMenu.html?Tab=true&ParentId=' + menuId, null, ($(document).height() - 300) / 2, 780, 300, '添加子菜单');
    });
    $('#aDelete').click(function() {
        deleteMenuById(menuId, '警告：您确定要删除【' + $('#menuPath').text() + '】和所有子菜单吗？\n\n该操作将无法恢复，请慎重选择！\n\n', 1);
    });
    $('#delMenu').click(function() {
        var menuIds = __grid.GetSelection('MenuId');
        if (menuIds.length > 0) {
            if (confirm('警告：您确定要删除选中菜单和所有子菜单吗？\n\n该操作将无法恢复，请慎重选择！\n\n')) {
                var delAjax = new top.AJAXFunction({ url: './service/1.3.5.1.9', param: { menuIds: menuIds} });
                delAjax.SuFun = function(json) {
                    if (json.d.Status == 10007) {
                        __grid.RefreshPage();
                        if (window.confirm("点击【确定】自动刷新当前页更新页面菜单显示！\n\n点击【取消】以后手动刷新页面更新页面菜单显示！"))
                            window.top.location.reload();
                    }
                };
                top.showMask();
                delAjax.SendFun();
            }
        }
        else
            alert('请选择需要删除的菜单');
    });
    $('#saveOrder').click(function() {
        var menuIds = __grid.GetSelection('MenuId');
        if (menuIds.length > 0) {
            var menuOrderNums = [];
            var flag = true;
            for (var i = 0; i < menuIds.length; i++) {
                menuOrderNums[i] = parseInt($('input[mid="' + menuIds[i] + '"]').val(), 10);
                if (isNaN(menuOrderNums[i]) || menuOrderNums[i] == null) {
                    flag = false;
                    break;
                }
            }
            if (flag) {
                var orderAjax = new top.AJAXFunction({ url: './service/1.3.5.1.10', param: { menuIds: menuIds, menuOrderNums: menuOrderNums} });
                orderAjax.SuFun = function(json) {
                    if (json.d.Status == 10011) {
                        __grid.RefreshPage();
                    }
                };
                orderAjax.SendFun();
            }
            else
                alert('请先设置正确的排序码');
        }
        else
            alert("请选择需要保存排序的菜单");
    });

    __grid = new GridPanel({
        store: new Store({
            url: '../service/1.3.5.1.5',
            params: { parentId: menuId }
        }
    ),
        renderTo: 'menuList',
        Columns: [
    { header: '菜单编号', dataIndex: 'MenuId', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '菜单名称', dataIndex: 'MenuName', sortable: false, allowHide: true, isShow: true, className: 'th01', render: '<span class="sp04"><a href="EditMenu.html?MenuId={MenuId}&FMenuId={ParentId}">{MenuName}</a><span class="sp04">' },
	{ header: '排序码', dataIndex: 'OrderNum', sortable: false, allowHide: true, isShow: true, className: 'th01', render: '<input mid="{MenuId}" onclick="this.select()" onkeypress="return checkNumber(event)"  onpaste="return !clipboardData.getData(\'text\').match(/\D/)" ondragenter="return false" class="inp01" maxlength="8" style="width: 30px; height: 16px; text-align: center;ime-mode:disabled" value="{OrderNum}"/>' },
	{ header: '左侧导航页', dataIndex: 'LeftUrl', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '右侧主页面', dataIndex: 'RightUrl', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '创建日期', dataIndex: 'CreateDate', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { return record.CreateDate.Value; } },
	{ header: '显示', dataIndex: 'Enabled', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { if (value) { return '<img src="../images/ch_yes.gif"/>' } else { return '<img src="../images/ch_no.gif"/>' } } },
	{ header: '操作', dataIndex: 'MenuId', allowHide: false, isShow: true, className: 'th01', render: '<span class="sp04"><a href="EditMenu.html?MenuId={MenuId}&FMenuId={ParentId}">[修改]</a><a href="javascript:;" onclick="deleteMenuById({MenuId}, \'警告：您确定要删除【{MenuName}】和所有子菜单吗？\\n\\n该操作将无法恢复，请慎重选择！\\n\\n\',0);">[删除]</a></span>' }
	]
    }
    );

    if (menuId > 0) {
        $('#aReset').click(function() {
            $('.listIB input.inp02').val('');
            $('#roleList input:checkbox').attr('checked', '');
        });

        $('#aEdit').click(function() {
            var keyCode = $('#trKeyCode').css('display') == 'none' ? 0 : $('#dplstKeyCode').find('option:selected').text().charCodeAt(0);
            var roleIdList = [];
            $('#roleList input:checkbox[checked=true]').each(function(i) {
                roleIdList[i] = $(this).val();
            });
            var editAjax = new top.AJAXFunction({ url: './service/1.3.5.1.6', data: jQuery.JSON.encode({ menuId: menuId, menuName: $('#txtName').val(), leftUrl: $('#txtLeftUrl').val(), rightUrl: $('#txtRightUrl').val(), keyCode: keyCode, enabled: $('input:radio[name="rdbtnEnabled"][checked=true]').val() == 1 ? true : false, enabledCloneToChildren: $('input:radio[name="rdbtnEnabledMode"][checked=true]').val() == 1 ? true : false, roleIdList: roleIdList, roleCloneToChildren: $('input:radio[name="rdbtnRoleMode"][checked=true]').val() == 1 ? true : false }) });
            editAjax.SuFun = function(json) {
                if (json.d.Status == 10001)
                    __grid.RefreshPage();
            };
            top.showMask();
            editAjax.SendFun();
        });

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
    else
        __grid.doRender();

    function loadMenuInfo() {
        var menuAjax = new top.AJAXFunction({ url: './service/1.3.5.1.4', data: '{menuId:' + menuId + '}' });
        menuAjax.SuFun = function(json) {
            var menuContent = json.d.Data;
            if (menuContent.ParentId == 0) {
                $('#trKeyCode').css('display', '');
                $('#dplstKeyCode').val(menuContent.KeyCode);
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
            }
            if (menuId > 0)
                $('#aBack').attr('href', 'EditMenu.html?MenuId=' + menuContent.ParentId);
            if (menuId > 0)
                $('#menuPath').html(menuContent.MenuPath);
            $('#txtName').val(menuContent.MenuName);
            $('#txtLeftUrl').val(menuContent.LeftUrl);
            $('#txtRightUrl').val(menuContent.RightUrl);
            $('#rdbtnEnabled_' + (menuContent.Enabled ? '0' : '1')).attr('checked', 'checked');
            $(menuContent.Roles).each(function(i) {
                $('input:checkbox[value="' + this.RoleId + '"]').attr('checked', 'checked');
            });
            __grid.doRender();
        };
        menuAjax.SendFun();
    }
}
);

function deleteMenuById(menuId, msg, flag) {
    if (confirm(msg)) {
        var delAjax = new top.AJAXFunction({ url: './service/1.3.5.1.8', data: '{ menuId: ' + menuId + '}' });
        delAjax.SuFun = function(json) {
            if (json.d.Status == 10007) {
                if (flag == 1) {
                    var url = $('#aBack').attr('href');
                    if (url && url.length > 0)
                        location = 'System/' + url;
                }
                else if (flag == 0) {
                    __grid.RefreshPage();
                    if (window.confirm("点击【确定】自动刷新当前页更新页面菜单显示！\n\n点击【取消】以后手动刷新页面更新页面菜单显示！"))
                        window.top.location.reload();
                }
            }
        };
        top.showMask();
        delAjax.SendFun();
    }
}

function checkNumber(event) {
    var key = event.keyCode || event.charCode;
    if (key == 8 || key == 46 || key == 37 || key == 39)
        return true;
    else
        return key >= 48 && key <= 57;
}