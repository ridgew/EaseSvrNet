///<reference path="../jQuery.js">
//M_ID, M_Name, M_BID, M_JavaTF, M_BrewTF, M_WAPTF, M_UACode, M_OrderNum, M_Info
//MT_ID
var query = jQuery.QueryString();
var grid = new GridPanel({
	store: new Store({
		url: '../service/1.3.9.2.5'
	}),
	renderTo: 'row3',
	Columns: [
	{ header: 'ID', dataIndex: 'M_ID', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '机型', dataIndex: 'M_Name', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '排序', dataIndex: 'M_OrderNum', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: 'JAVA支持', dataIndex: 'M_JavaTF', sortable: false, allowHide: true, isShow: true, className: 'th01', render: { 'true': '<span class="spYes"></span>', 'false': '<span class="spNo"></span>'} },
	{ header: 'Brew支持', dataIndex: 'M_BrewTF', sortable: false, allowHide: true, isShow: true, className: 'th01', render: { 'true': '<span class="spYes"></span>', 'false': '<span class="spNo"></span>'} },
	{ header: 'WAP支持', dataIndex: 'M_WAPTF', sortable: false, allowHide: true, isShow: true, className: 'th01', render: { 'true': '<span class="spYes"></span>', 'false': '<span class="spNo"></span>'} },
	{ header: 'Mobile支持', dataIndex: 'M_MobileTF', sortable: false, allowHide: true, isShow: true, className: 'th01', render: { 'true': '<span class="spYes"></span>', 'false': '<span class="spNo"></span>'} },
	{ header: 'WinCE支持', dataIndex: 'M_WinceTF', sortable: false, allowHide: true, isShow: true, className: 'th01', render: { 'true': '<span class="spYes"></span>', 'false': '<span class="spNo"></span>'} },
	{ header: 'UA编码', dataIndex: 'M_UACode', sortable: false, allowHide: true, isShow: false, className: 'th01' },
	{ header: '其他说明', dataIndex: 'M_Info', sortable: false, allowHide: true, isShow: false, className: 'th01' }
	]
});
grid.store.GetPageParams();
jQuery(function($) {
	if (query['platform']) {
		grid.store.url = '../service/1.3.9.2.13';
		var rightWindow = top.deskTabs.getFrameRight().contentWindow;
		grid.store.params.mid = rightWindow.devicesGrid.GetDataByIndex('M_ID');
		grid.store.params.platform = parseInt(grid.store.params.platform, 10);
	}
	else if (query.MT_ID) {
		if (query['import']) {
			grid.store.url = '../service/1.3.9.2.9';
		} else {
			grid.store.url = '../service/1.3.9.2.8';
		}

	} else {
		grid.Columns.push({ header: '操作', dataIndex: 'M_ID', isShow: true, className: 'th01',
			render: function(value, record) {
				var tools = $('<span class="sp04"></span>');
				tools.append('<a href="javascript:void(0)">[修改]</a>').click(function(e) {
					e.preventDefault();
					top.fn_add_div('./Device/MobileEdit.html?M_ID=' + value, '', '', '700px', '420px', '修改机型');
				});
				return tools[0];
			}
		});
		grid.Columns[2].render = '<input type="text" value="{M_OrderNum}" MID="{M_ID}" onkeyup="this.value=this.value.replace(/[^\\d]/g,\'\')" name="txtM_OrderNum" style="width: 60px; height: 18px; text-align: center;" class="inp02"/>';
		
	}
	grid.doRender();

	$('#btnRefreshPage1').click(function(e) {
		e.preventDefault();
		grid.RefreshPage();
	});
});