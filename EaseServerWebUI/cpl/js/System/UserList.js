/// <reference path="../jquery.js" />
var __grid;
$(document).ready(function() {
    $('#aAdd').click(function() {
        top.fn_add_div('./System/CreateUser.html', null, ($(document).height() - 450) / 2, 780, 450, '添加新用户');
    });
    $('#aDelete').click(deleteUser);
    $('#aEdit').click(function() {
        var userIds = __grid.GetSelection('UserId');
        if (userIds.length > 0)
            top.fn_add_div('./System/EditUsers.html?ids=' + userIds, null, ($(document).height() - 280) / 2, 780, 280, '批量修改用户');
    });

    __grid = new GridPanel({
        store: new Store({ url: '../service/1.3.5.3.2' }),
        renderTo: 'userList',
        Columns: [
    { header: '用户编号', dataIndex: 'UserId', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '用户名', dataIndex: 'UserName', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '真实姓名', dataIndex: 'RealName', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '电子邮箱', dataIndex: 'Email', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '移动电话', dataIndex: 'Mobile', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '审核', dataIndex: 'IsApproved', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { if (value) { return '<img src="../images/ch_yes.gif"/>' } else { return '<img src="../images/ch_no.gif"/>' } } },
	{ header: '锁定', dataIndex: 'IsLockedOut', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { if (value) { return '<img src="../images/ch_no.gif"/>' } else { return '<img src="../images/ch_yes.gif"/>' } } },
	{ header: '注册时间', dataIndex: 'CreateDate', sortable: false, allowHide: true, isShow: false, className: 'th01', render: function(value, record) { return record.CreateDate.Value; } },
	{ header: '注册IP', dataIndex: 'CreateIP', sortable: false, allowHide: true, isShow: false, className: 'th01' },
	{ header: '最近登录时间', dataIndex: 'LastLoginDate', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { return record.LastLoginDate.Value; } },
	{ header: '最近登录IP', dataIndex: 'LastLoginIP', sortable: false, allowHide: true, isShow: false, className: 'th01' },
	{ header: '登陆次数', dataIndex: 'LoginCount', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '最近更改密码时间', dataIndex: 'LastPasswordChangedDate', sortable: false, allowHide: true, isShow: false, className: 'th01', render: function(value, record) { return record.LastPasswordChangedDate.Value; } },
	{ header: '最近锁定时间', dataIndex: 'LastLockedoutDate', sortable: false, allowHide: true, isShow: false, className: 'th01', render: function(value, record) { return record.LastLockedoutDate.Value; } },
	{ header: '最近密码连续失败次数', dataIndex: 'FailedPasswordAttemptCount', sortable: false, allowHide: true, isShow: false, className: 'th01' },
	{ header: '最近密码失败时间', dataIndex: 'FailedPasswordAttemptWindowStart', sortable: false, allowHide: true, isShow: false, className: 'th01', render: function(value, record) { return record.FailedPasswordAttemptWindowStart.Value; } },
	{ header: '备注', dataIndex: 'Comment', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '操作', dataIndex: 'UserId', allowHide: false, isShow: true, className: 'th01', render: '<span class="sp04"><a href="javascript:;" onclick="editUser({UserId})">[修改]</a><a href="javascript:;" onclick="deleteUser({UserId});">[删除]</a></span>' }
	]
    }
    );
    __grid.doRender();
});

function editUser(userId) {
    top.fn_add_div('./System/EditUser.html?userId=' + userId, null, ($(document).height() - 450) / 2, 780, 450, '修改用户');
}

function deleteUser(userId) {
    if (userId && userId > 0) {
        if (confirm('警告：您确定要删除该用户吗？\n\n该操作将无法恢复，请慎重选择！\n\n')) {
            var delAjax = new top.AJAXFunction({ url: './service/1.3.5.3.6', data: '{ userId: ' + userId + '}' });
            delAjax.SuFun = function(json) {
                if (json.d.Status == 10025)
                    __grid.RefreshPage();
            };
            top.showMask();
            delAjax.SendFun();
        }
    }
    else {
        var userIds = __grid.GetSelection('UserId');
        if (userIds.length > 0) {
            if (confirm('警告：您确定要删除所有选中用户吗？\n\n该操作将无法恢复，请慎重选择！\n\n')) {
                var delAjax = new top.AJAXFunction({ url: './service/1.3.5.3.7', param: { userIds: userIds} });
                delAjax.SuFun = function(json) {
                    if (json.d.Status == 10025) {
                        __grid.RefreshPage();
                    }
                };
                top.showMask();
                delAjax.SendFun();
            }
        }
    }
}