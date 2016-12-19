/// <reference path="../jquery.js" />
$(document).ready(function() {
    var userId = $.QueryString().userId;
    
    $('#aReset').click(function() {
        $('.listIB input.inp02').not('#txtUserName').val('');
        $('#roleList input:checkbox').attr('checked', '');
        $('#txtComment').val('');
    });

    $('#aSave').click(function() {
    
        
        
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
        var saveAjax = new top.AJAXFunction({ url: './service/1.3.5.3.5', param: { userId: userId, password: pwd, realname: realname, email: email, mobile: mobile, isApproved: isApproved, isLockedOut: isLockedOut, lockedoutDate: lockedoutDate, comment: comment, roleNames: roleNames} });
        saveAjax.SuFun = function(json) {
            if (json.d.Status == 10023)
            {
                
                    top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
                
            }
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

        var getAjax = new top.AJAXFunction({ url: './service/1.3.5.3.4', param: { userId: userId} });
        getAjax.SuFun = function(json) {
            json = json.d.Data;
            $('#txtUserName').val(json.UserName);
            $('#txtRealName').val(json.RealName);
            $('#txtEmail').val(json.Email);
            $('#txtMobile').val(json.Mobile);
            $('#txtRealName').val(json.RealName);
            $('#chkbxApproved_' + (json.IsApproved ? '1' : '0')).attr('checked', 'checked');
            $('#chkbxLockedOut_' + (json.IsLockedOut ? '1' : '0')).attr('checked', 'checked');
            $('#txtLastLockedOutDate').val(json.LastLockedoutDate.Value);
            $('#txtComment').val(json.Comment);
            $(json.Roles).each(function(i) {
                $('input:checkbox[value="' + this + '"]').attr('checked', 'checked');
            });
        };
        getAjax.SendFun();
    };
    roleAjax.SendFun();
    
    
    
});