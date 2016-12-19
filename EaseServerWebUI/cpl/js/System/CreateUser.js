/// <reference path="../jquery.js" />
$(document).ready(function() {
    $('#aReset').click(function() {
        $('.listIB input.inp02').val('');
        $('#roleList input:checkbox').attr('checked', '');
        $('#txtComment').val('');
    });
    $('#aSave').click(function() {
        var username = $('#txtUserName').val();
        var pwd = $('#txtPassword').val();
        var repwd = $('#txtRePassword').val();
        var realname = $('#txtRealName').val();
        var email = $('#txtEmail').val();
        var mobile = $('#txtMobile').val();
        var isApproved = $('input:radio[name="chkbxApproved"][checked=true]').val() == 1 ? true : false;
        var isLockedOut = $('input:radio[name="chkbxLockedOut"][checked=true]').val() == 1 ? true : false;
        var lockedoutDate = $('#txtLastLockedOutDate').val();
        var comment = $('#txtComment').val();

        if (repwd != pwd) {
            alert('两次输入的密码不一致，请重试！');
            return;
        }
        if (lockedoutDate.length == 0)
            lockedoutDate = '0001-1-1 0:00:00';
        else {
            var reg = new RegExp('^((\\d{4})-((0?[1-9])|(1[0-2]))-(\\d|([0-2]\\d)|(3[0-1]))\\s((0?|1)\\d|2[0-3])):(0?|[1-5])\\d(:(0?|[1-5])\\d(\\.\\d{3})?)?$');
            if (!reg.test(lockedoutDate)) {
                alert('锁定到期时间格式不正确');
                return;
            }
        }

        var roleNames = [];
        $('#roleList input:checkbox[checked=true]').each(function(i) {
            roleNames[i] = $(this).val();
        });
        var saveAjax = new top.AJAXFunction({ url: './service/1.3.5.3.3', param: { username: username, password: pwd, realname: realname, email: email, mobile: mobile, isApproved: isApproved, isLockedOut: isLockedOut, lockedoutDate: lockedoutDate, comment: comment, roleNames: roleNames} });
        saveAjax.SuFun = function(json) {
            if (json.d.Status == 10019)
                top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
        };
        top.showMask();
        saveAjax.SendFun();
    });

    var roleAjax = new top.AJAXFunction({ url: './service/1.3.5.2.1' });
    roleAjax.SuFun = function(json) {
        var roleList = json.d.Data;
        $(roleList).each(function(i) {
            $('#roleList').append('<input value="' + this.RoleName + '" id="chkbxRole_' + i + '" name="chkbxRole$' + i + '" type="checkbox"><label for="chkbxRole_' + i + '">' + this.Description + '</label>&nbsp;');
        });
    };
    roleAjax.SendFun();
});