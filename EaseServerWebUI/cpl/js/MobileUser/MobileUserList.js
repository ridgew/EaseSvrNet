//手机用户信息列表部分JS
//获取列表数据
var grid = new GridPanel({
    store: new Store({url: '../service/1.3.6.4.1', maskPanel:'#row3'}),
    renderTo: 'row3',
    Columns: [
	{ header: '编号', dataIndex: 'SOFTWARE_ID', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '业务编号', dataIndex: 'SERVICE_ID', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '设备编号', dataIndex: 'DEVICE_ID', sortable: false, allowHide: false, isShow: true, className: 'th01' },
//	{ header: '用户姓名', dataIndex: 'USER_NAME', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '手机号码', dataIndex: 'MSID', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '手机卡号', dataIndex: 'IMEI', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '机身编号', dataIndex: 'IMSI', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '性别', dataIndex: 'USER_SEX', sortable: false, allowHide: false, isShow: true, className: 'th01', render: { '1': '女', '0': '男'} },
	{ header: '年龄', dataIndex: 'USER_AGE', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '证件类型', dataIndex: 'USER_CARD_TYPE', sortable: false, allowHide: false, isShow: true, className: 'th01', render: { '1': '身份证', '2': '护照', '3': '军官证', '4': '其他'} },
	{ header: '证件号码', dataIndex: 'USER_ID_CARD', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '地址', dataIndex: 'USER_ADDR', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '首次访问日期', dataIndex: 'FIRST_VISIT_TIME', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '操作', dataIndex: 'SOFTWARE_ID', isShow: true, className: 'th01', render: '<span class="sp04"><a href="javascript:GetMobileUserInfoByID({SOFTWARE_ID});">[查看详情]</a></span>' }
	],
    cellSelection: false,
    rowSelection: false
});


//页面参数配置
//SearchField,SearchCondition,SearchValue说明
//SearchField为字符串数组，为搜索的字段名
//SearchCondition为数字数组，为搜索的条件：0:"=",1:"<=",2:">=",3:"<",4:">",5:"<>",6:"like",7:"charindex"
//SearchValue为OBJECT数组，为搜索的值得
//三个数组的元素必须一直互相对应。
var pageConfig = { SearchField: ["SOFTWARE_ID"], SearchCondition: [4], SearchValue: [0] };


//页面加载执行
$(function() {
    //初始化页面信息
    Init_PageDataListConfig();

    //初始化页面按钮事件
    Init_PageButton();

});


//初始化请求条件
function Init_PageDataListConfig() {
    //添加查询到列表请求中
    grid.store.params.SearchField = pageConfig.SearchField;
    grid.store.params.SearchCondition = pageConfig.SearchCondition;
    grid.store.params.SearchValue = pageConfig.SearchValue;

    grid.autoLoad = false;
    grid.onSuccess = function(data) {
        if (!this.rendered) {
            this.DrawTableHead();
            this.FitHeight();
        }
        this.DrawTable(data);
        this.onDataChanged(data);
    }
    grid.store.params.pageIndex = 1;
    grid.store.load();
}


//初始化页面按钮事件
function Init_PageButton() {
    //刷新按钮
    $("#btnRefreshPage1").click(function() {
        grid.RefreshPage();
    });

    //搜索按钮
    $("#btn_Search").click(function() {
        top.fn_add_div('./MobileUser/MobileUserInfoSearch.html', '', '', '610px', '210px', '用户搜索');
    });
}


//查看用户详细信息
function GetMobileUserInfoByID(id) {
    top.fn_add_div('./MobileUser/MobileUserInfo.html?id=' + id, '', '100px', '670px', '245px', '查看用户详细信息');
}