var Store = function(config) {
	config = config || {};
	this.url = config.url || '';
	this.type = config.type || 'POST';
	this.params = config.params || {};
	this.mask = config.mask || '数据加载中...';
	this.reader = config.reader || { dataRoot: 'Data', pages: 'PageCount', records: 'RecordCount', jsonRoot: 'd', pageSize: 'PageSize' };
	this.async = config.async || true;
	this.contentType = config.contentType || 'application/json';
	this.data = null;
	this.pages = 0;
	this.records = 0;
	var _self = this;
	this.load = function(cfg) {
		if (cfg) {
			if (cfg.pageIndex) this.params.pageIndex = parseInt(cfg.pageIndex, 10);
			if (cfg.pageSize) this.params.pageSize = parseInt(cfg.pageSize, 10);
		}
		if (this.url) {
			if (config.maskPanel && $.blockUI)
			{
				$(config.maskPanel).block({ message: this.mask, css: { width: "30%", left: "70%", top: "10%"} });
			}
			else
			{
				top.showMask(this.mask);
			}
			$.ajax({
				type: this.type, url: this.url, dataType: 'json', cache: false,
				data: $.JSON.encode(this.params),
				async: this.async,
				contentType: this.contentType,
				error: function(xhr, status, thrown) {
					if (status == 'parsererror') {
						top.window.location.href = top.window.location.href.replace('index.html', 'login.html');
					} else if (status == 'error') {
						alert('服务器内部错误');
					} else {
						_self.onFailure(xhr, status, thrown);
					}
				},
				success: function(data, status) {
					if (typeof _self.reader["jsonRoot"] != "undefined" && _self.reader["jsonRoot"] != "") {
						_self.data = data[_self.reader.jsonRoot];
					} else {
						_self.data = data;
					}

					if (typeof (_self.data[_self.reader.pages]) != "undefined") {
						_self.pages = parseInt(_self.data[_self.reader.pages], 10);
					}
					if (typeof (_self.data[_self.reader.records]) != "undefined") {
						_self.records = parseInt(_self.data[_self.reader.records], 10);
					}
					var datSource = eval('_self.data.' + _self.reader.dataRoot);
					if (typeof (datSource) != "undefined") {
						_self.onSuccess(datSource, status);
					} else {
						_self.onSuccess(_self.data, status);
					}
				},
				complete: function(xhr, status) {
					if ($.blockUI && config.maskPanel){$(config.maskPanel).unblock();} else { top.hideMask(); }
					_self.onComplete(xhr, status);
				}
			});
		} else {
			this.onSuccess(this.data);
		}
	};
	//获取URL参数
	this.GetPageParams = function() {
		var pageParams;
		if (arguments.length > 0) {
			pageParams = $.QueryString(arguments[0]);
		} else {
			pageParams = $.QueryString();
		}
		for (var field in pageParams) {
			this.params[field] = pageParams[field];
		}
	};
	this.onFailure = function() { };
	this.onSuccess = function() { };
	this.onComplete = function() { };
};

var GridPanel = function(config) {
	config = config || {};
	this.renderTo = config.renderTo || '';
	this.Columns = config.Columns || [];
	this.store = config.store;
	this.autoLoad = true;
	this.rendered = false;
	this.element = null;
	this.cellSelection = true;
	this.rowSelection = true;
	this.singleSelect = false;
	this.pageNavigation = true;
	this.autoHeight = true;
	this.PageNavPanel = null;
	this.selected = {};
	if (typeof (config.cellSelection) != 'undefined') {
		this.cellSelection = config.cellSelection;
	}
	if (typeof (config.rowSelection) != 'undefined') {
		this.rowSelection = config.rowSelection;
	}
	if (typeof (config.singleSelect) != 'undefined') {
		this.singleSelect = config.singleSelect;
	}
	if (typeof (config.pageNavigation) != 'undefined') {
		this.pageNavigation = config.pageNavigation;
	}
	if (typeof (config.autoHeight) != 'undefined') {
		this.autoHeight = config.autoHeight;
	}
	if (typeof (config.autoLoad) != 'undefined') {
		this.autoLoad = config.autoLoad;
	}
	this.set_PageIndex = function(index) {
		if (isNaN(index)) {
			throw "参数类型不正确";
		}
		index = parseInt(index, 10);
		if (this.store.pages < 1) {
			this.store.params.pageIndex = 1;
		} else {
			if (index > 0 && index <= this.store.pages) {
				this.store.params.pageIndex = index;
			} else {
				throw "参数超出范围";
			}
		}
	};
	this.FirstPage = function() {
		this.store.params.pageIndex = 1;
		this.store.load();
	};
	this.NextPage = function() {
		this.store.params.pageIndex++;
		this.store.load();
	};
	this.PreviousPage = function() {
		this.store.params.pageIndex--;
		this.store.load();
	};
	this.LastPage = function() {
		this.store.params.pageIndex = this.store.pages;
		this.store.load();
	};
	this.ResetPageSize = function(val) {
		this.store.params.pageSize = parseInt(val, 10);
		this.store.params.pageIndex = 1;
		this.store.load();
		top.$.cookie('UserPageSize', this.store.params.pageSize);
	};
	this.GoPageIndex = function(val) {
		this.store.params.pageIndex = parseInt(val, 10);
		if (this.store.params.pageIndex > this.store.pages) {
			this.store.params.pageIndex = this.store.pages;
		}
		this.store.load();
	};
	this.Refresh = this.RefreshPage = function() {
		//		if (this.autoLoad) {
		this.store.load();
		//		} else {
		//			var data = eval('this.store.data.' + this.store.reader.dataRoot);
		//			if (data != null) {
		//				this.DrawTable(data);
		//			}
		//		}
	};
	var _self = this;
	this.id = null;

	this.GetID = function() {
		for (var i = 0; i < 1000; i++) {
			if (document.getElementById("_pnlGrid" + i.toString(10))) {
				this.id = "_pnlGrid" + i.toString(10);
				break;
			}
		}
	};
	if (this.pageNavigation) {
		//初始化分页号和分页大小
		this.store.params.pageIndex = 1;
		var userPageSize = top.$.cookie('UserPageSize');
		if (userPageSize) {
			this.store.params.pageSize = parseInt(userPageSize, 10);
		} else {
			this.store.params.pageSize = 0;
		}
	}
	this.doRender = function() {
		if (this.element == null || this.element.parentNode == null || this.element.parentNode.nodeType != 1) {
			this.DrawTableHead();
		}
		if (this.autoHeight) {
			this.FitHeight();
		}
		if (this.autoLoad) {
			this.store.load();
		} else {
			var data = eval('this.store.data.' + this.store.reader.dataRoot);
			if (data != null) {
				this.DrawTable(data);
			}
		}
	};
	this.DrawTableHead = function() {
		if (typeof this.renderTo == 'string') {
			this.id = this.renderTo;
			this.renderTo = document.getElementById(this.id);
			if (!this.renderTo) {
				this.renderTo = document.body.appendChild(document.createElement('div'));
			}

		} else if (this.renderTo.ownerDocument) {
			this.id = this.renderTo.id;
			if (!this.id) {
				this.GetID();
			}
		}
		if (!this.id) {
			throw "请为对象 renderTo 指定id属性";
		} else {
			this.renderTo.id = this.id;
		}
		this.element = document.createElement('table');
		this.element.setAttribute('cellSpacing', '1');
		this.element.setAttribute('cellPadding', '0');
		this.element.setAttribute('border', '0');
		this.element.className = 'listCh1';
		$(this.element).width('auto');
		//创建列选择UI
		if (this.cellSelection) {
			var columnUI = document.body.appendChild(document.createElement('div'));
			columnUI.id = _self.id + "selectColumn";
			columnUI.className = "tableText";
			columnUI.style.display = "none";
			columnUI.style.width = "auto";
			columnUI.left = "0px";
			columnUI.top = "0px";
			var times;
			columnUI.onmouseout = function() {
				times = window.setTimeout("$('#" + _self.id + "selectColumn').hide();", 500);
			};
			columnUI.onmouseover = function() {
				window.clearTimeout(times);
			};
			columnUI.cellCount = 0;
			this.columnUI = columnUI;
		}
		//-----------
		var headRow = this.element.insertRow(-1);
		if (this.rowSelection) {
			var checkCell = document.createElement('th');
			checkCell.className = 'th11';
			checkCell.style.whiteSpace = 'nowrap';
			if (!this.singleSelect) {
				var checkAllbox = document.createElement('input');
				checkAllbox.type = "checkbox";
				checkAllbox.name = checkAllbox.id = _self.id + "_selAllRow";
				checkAllbox.onclick = function() {
					var sels = _self.element.getElementsByTagName('input');

					for (var num = 0; num < sels.length; num++) {
						if (sels[num].type == 'checkbox' && sels[num].name == _self.id + '__selRow') {
							try {
								sels[num].checked = this.checked
								if (this.checked) { _self.selected[sels[num].value] = true; }
							} catch (ex) {
							}
						}
					}
					if (!this.checked) { _self.selected = {}; }
				};
				checkCell.appendChild(checkAllbox);
			}
			headRow.appendChild(checkCell);
		}


		var tmpTh;
		for (var num = 0; num < this.Columns.length; num++) {
			tmpTh = document.createElement('th');
			tmpTh.innerHTML = this.Columns[num].header;
			if (this.Columns[num].sortable) {
				var tmpSortUI = document.createElement('img');
				tmpSortUI.src = '../images/ch_right_ico06.gif';
				tmpSortUI.className = 'im01';
				tmpSortUI.column = this.Columns[num].dataIndex;
				tmpSortUI.style.cursor = 'pointer';
				tmpSortUI.onclick = function() {
					_self.store.params['orderField'] = [this.column];
					if (this.src.indexOf('ch_right_ico06') > -1) {
						_self.store.params['orderbyDesc'] = [true];
						this.src = '../images/ch_right_ico07.gif';
					} else {
						_self.store.params['orderbyDesc'] = [false];
						this.src = '../images/ch_right_ico06.gif';
					}
					_self.store.load({ pageIndex: 1 });
				};
				tmpTh.appendChild(tmpSortUI);
			}
			tmpTh.className = this.Columns[num].className;
			tmpTh.style.whiteSpace = 'nowrap';

			if (this.Columns[num].isShow) {
				tmpTh.style.display = "";
			} else {
				tmpTh.style.display = "none";
			}
			headRow.appendChild(tmpTh);
			//加入列选择列表
			if (this.Columns[num].allowHide && this.cellSelection) {
				var tmpP = document.createElement('p');
				var tmpLable = document.createElement('label');
				var tmpInput = document.createElement('input');
				tmpInput.type = "checkbox";
				tmpInput.name = _self.id + "_" + this.Columns[num].dataIndex;
				tmpInput.value = num;

				tmpInput.onclick = function() {
					_self.Columns[this.value].isShow = this.checked;
					for (var n = 0; n < this.cells.length; n++) {
						this.cells[n].style.display = this.checked ? "" : "none";
					}
				};
				tmpLable.appendChild(tmpInput);
				tmpLable.appendChild(document.createTextNode(this.Columns[num].header));
				tmpP.appendChild(tmpLable);
				columnUI.appendChild(tmpP);
				tmpInput.checked = this.Columns[num].isShow;
				columnUI.cellCount++;
				tmpInput.cells = [tmpTh];
			}
		}

		if (this.cellSelection && columnUI.cellCount > 0) {
			tmpTh = document.createElement('th');
			tmpTh.innerHTML = '&gt;&gt;';
			tmpTh.className = 'th11';
			tmpTh.style.whiteSpace = 'nowrap';
			tmpTh.style.cursor = 'pointer';
			headRow.appendChild(tmpTh);
			tmpTh.onclick = function() {
				$(columnUI).toggle();
				$(columnUI).css('left', 0);
				$(columnUI).css('top', 0);
				var columnUIWidth = $(columnUI).outerWidth(true);
				var columnUIHeight = $(columnUI).outerHeight(true);
				var thWidth = $(this).outerWidth(true);
				var thHeight = $(this).outerHeight(true);
				var pos = $(this).offset();
				if (columnUIWidth > thWidth) {
					left = pos.left - (columnUIWidth - thWidth);
				}
				if ((pos.top + thHeight + columnUIHeight) > $(document.body).height()) {
					left = left - 17;
				}
				$(columnUI).css('left', left);
				$(columnUI).css('top', pos.top + $(this).outerHeight(true));
				return false;
			};
			$(tmpTh).css('cursor', 'pointer').mouseout(function() { times = window.setTimeout("$('#" + _self.id + "selectColumn').hide();", 1000); });
		}
		this.element.tBodies[0].className = 'tb1';
		$(this.renderTo).css('overflow', 'auto');
		this.renderTo.appendChild(this.element);
		if (this.pageNavigation) {
			//var pageNav = this.renderTo.parentNode.appendChild(document.createElement('div'));
			var pageNav = document.body.appendChild(document.createElement('div'));
			pageNav.className = 'rightTurn';
			pageNav.id = _self.id + 'PageNavPanel';
			pageNav.innerHTML = '<span class="tr1">每页显示条数<select id="' + _self.id + 'selResetPageSize" name="' + _self.id + 'selResetPageSize" class="sel01"><option value="5">5</option><option value="10">10</option><option value="15">15</option><option value="20">20</option><option value="25">25</option><option value="30">30</option><option value="35">35</option><option value="40">40</option><option value="45">45</option><option value="50">50</option></select></span><span class="t01" id="' + _self.id + 'btnFirstPage" title="第一页"></span><span class="t02" id="' + _self.id + 'btnPreviousPage" title="前一页"></span><span class="tlin"></span><span class="te">第<input name="' + _self.id + 'txtPageIndex" type="text" class="inp01" id="' + _self.id + 'txtPageIndex" value="1" maxlength="10" />页</span><span class="tr3" id="' + _self.id + 'btnGoPageIndex" title="转到该页"></span><span class="te">共<font id="' + _self.id + 'lblPageCount">1</font>页，共<font id="' + _self.id + 'lblRowCount">1</font>条</span><span class="tlin"></span><span class="t03" id="' + _self.id + 'btnNextPage" title="后一页"></span><span class="t04" id="' + _self.id + 'btnLastPage" title="最后页"></span><span class="tlin"></span><span class="tsx" id="' + _self.id + 'btnRefresh" title="刷新"></span><div class="clear"></div>';
			if (this.store.params.pageSize > 0) {
				$('#' + _self.id + 'selResetPageSize').val(this.store.params.pageSize);
			}
			//绑定按钮事件
			$('#' + _self.id + 'selResetPageSize').change(function() {
				_self.ResetPageSize(this.value);
			});
			$('#' + _self.id + 'txtPageIndex').keypress(function(e) {
				if (e.keyCode == 13) {
					if (this.value < 1 || isNaN(this.value)) {
						this.value = _self.store.params.pageIndex;
					} else {
						_self.GoPageIndex(parseInt(this.value, 10));
					}
				}
			});
			$('#' + _self.id + 'btnGoPageIndex').click(function(e) {
				var pindex = $('#' + _self.id + 'txtPageIndex').val();
				if (pindex < 1 || isNaN(pindex)) {
					$('#' + _self.id + 'txtPageIndex').val(_self.store.params.pageIndex);
				} else {
					_self.GoPageIndex(parseInt(pindex, 10));
				}
			});
			$('#' + _self.id + 'btnRefresh').click(function() {
				_self.RefreshPage();
			});
			this.PageNavPanel = pageNav;
		}
		this.rendered = true;
	};
	this.setPageConfig = function() {
		if (this.store.params.pageIndex > this.store.pages && this.store.pages > 0) {
			this.store.params.pageIndex = this.store.pages;
			this.Refresh();
			return false;
		}
		document.getElementById('' + _self.id + 'txtPageIndex').value = this.store.params.pageIndex;
		document.getElementById('' + _self.id + 'lblPageCount').innerHTML = this.store.pages;
		document.getElementById('' + _self.id + 'lblRowCount').innerHTML = this.store.records;
		if (this.store.params.pageIndex <= 1) {
			$('#' + _self.id + 'btnFirstPage')[0].className = "t01";
			$('#' + _self.id + 'btnPreviousPage')[0].className = "t02";
			$('#' + _self.id + 'btnNextPage')[0].className = "t13";
			$('#' + _self.id + 'btnLastPage')[0].className = "t14";
			$('#' + _self.id + 'btnFirstPage')[0].onclick = function() { };
			$('#' + _self.id + 'btnPreviousPage')[0].onclick = function() { };
			$('#' + _self.id + 'btnNextPage')[0].onclick = function() { _self.NextPage(); };
			$('#' + _self.id + 'btnLastPage')[0].onclick = function() { _self.LastPage(); };
		} else {
			$('#' + _self.id + 'btnFirstPage')[0].className = "t11";
			$('#' + _self.id + 'btnPreviousPage')[0].className = "t12";
			$('#' + _self.id + 'btnNextPage')[0].className = "t13";
			$('#' + _self.id + 'btnLastPage')[0].className = "t14";

			$('#' + _self.id + 'btnFirstPage')[0].onclick = function() { _self.FirstPage(); };
			$('#' + _self.id + 'btnPreviousPage')[0].onclick = function() { _self.PreviousPage(); };
			$('#' + _self.id + 'btnNextPage')[0].onclick = function() { _self.NextPage(); };
			$('#' + _self.id + 'btnLastPage')[0].onclick = function() { _self.LastPage(); };
		}
		if (this.store.params.pageIndex >= this.store.pages) {
			$('#' + _self.id + 'btnNextPage')[0].className = "t03";
			$('#' + _self.id + 'btnLastPage')[0].className = "t04";
			$('#' + _self.id + 'btnNextPage')[0].onclick = function() { };
			$('#' + _self.id + 'btnLastPage')[0].onclick = function() { };
		}
		//this.setNavigation();
	}
	this.FitHeight = function() {
		var tElement = this.element;
		var winObj = this.element.ownerDocument.parentWindow;
		if (!winObj) {
			winObj = this.element.ownerDocument.defaultView;
		}
		winObj.onresize = function() {
			//top.$('#pnlToldPanel').show();
			//top.$('#pnlToldBody').html(new Date().toTimeString() + '->' + $(tElement.ownerDocument.body).outerHeight(true) + '<br />' + top.$('#pnlToldBody').html());
			$(tElement.ownerDocument.body).width('auto');
			//$(tElement.parentNode).height('auto');
			$(tElement.ownerDocument.body).height('auto');
			var newHeight = $(tElement.parentNode).height() + ($(this).height() - $(tElement.ownerDocument.body).outerHeight(true));
			if (newHeight < 51) {
				newHeight = 'auto'; $(tElement.ownerDocument.body).width($(tElement.ownerDocument.body).width() - 17);
			}
			$(tElement.parentNode).height(newHeight);
		};
		if (winObj.frameElement) {
			winObj.frameElement.onresize = function() { winObj.onresize() };
			winObj.onunload = function() { winObj.frameElement ? winObj.frameElement.onresize = function() { } : winObj.onresize = function() { }; };
		}
		winObj.onresize();
		this.autoHeight = false;
	};
	this.onSuccess = function(data) {
		this.DrawTable(data);
		this.onDataChanged(data);
	};
	this.onRowChecked = function(e) {
		e.stopPropagation();
		if (_self.singleSelect) {
			if (this.checked) {
				_self.selected = {};
				_self.selected[this.value] = this.checked;
			}
		} else {
			if (!this.checked) {
				delete _self.selected[this.value];
			} else {
				_self.selected[this.value] = this.checked;
			}
		}
	}
	this.DrawTable = function(data) {
		if (data) {
			if (this.cellSelection && this.columnUI.cellCount > 0) {
				$($(this.columnUI).find('input')).each(function(index, value) {
					value.cells.splice(1, value.cells.length - 1);
				});
			}
			for (var i = this.element.rows.length - 1; i > 0; i--) {
				this.element.deleteRow(i);
			}

			for (var i = 0; i < data.length; i++) {
				var tmpRow = this.element.insertRow(-1);
				if (i % 2 == 0) {
					tmpRow.className = "atr";
					tmpRow.onmouseover = function() { this.className = "ctr"; };
					tmpRow.onmouseout = function() { this.className = "atr"; };

				} else {
					tmpRow.className = "btr";
					tmpRow.onmouseover = function() { this.className = "ctr"; };
					tmpRow.onmouseout = function() { this.className = "btr"; };
				}
				if (this.rowSelection) {
					var checkCell = document.createElement('td');
					checkCell.className = 'th11';
					checkCell.style.whiteSpace = 'nowrap';
					var checkRowbox;
					try {
						checkRowbox = document.createElement('<input name="' + _self.id + '__selRow" id="' + _self.id + "__selRow" + i.toString(10) + '" value="' + i.toString(10) + '">');
					} catch (ex) {
						checkRowbox = document.createElement('input');
						checkRowbox.name = _self.id + "__selRow";
						checkRowbox.id = _self.id + "__selRow" + i.toString(10);
						checkRowbox.value = i;
					}
					if (this.singleSelect) {
						checkRowbox.type = 'radio';
					} else {
						checkRowbox.type = "checkbox";
					}
					checkCell.appendChild(checkRowbox);
					$(checkRowbox).click(this.onRowChecked);
					checkRowbox = null;
					tmpRow.appendChild(checkCell);
				}
				for (var field = 0; field < this.Columns.length; field++) {
					var tmpCol = document.createElement("td");
					var tmpHTML = '';
					if (typeof (this.Columns[field].render) != "undefined") {
						switch (typeof (this.Columns[field].render)) {
							case 'string':
								tmpHTML = this.Columns[field].render;
								var matchs = tmpHTML.match(/\{.*?\}/g);
								if (matchs) {
									for (var j = 0; j < matchs.length; j++) {
										tmpHTML = tmpHTML.replace(matchs[j], data[i][matchs[j].substring(1, matchs[j].length - 1)]);
									}
								}
								break;
							case 'function':
								tmpHTML = this.Columns[field].render(data[i][this.Columns[field].dataIndex], data[i], i);
								break;
							case 'object':
								tmpHTML = this.Columns[field].render[data[i][this.Columns[field].dataIndex]];
								break;
						}
					} else {
					    tmpHTML = data[i][this.Columns[field].dataIndex];
					}
					if (tmpHTML && typeof tmpHTML == 'object' && tmpHTML.ownerDocument == tmpCol.ownerDocument) {
					    tmpCol.appendChild(tmpHTML);
					} else {
					    tmpCol.innerHTML = !(tmpHTML) ? "" : tmpHTML.toString().replace("\r", "").replace("\n", "<br/>");
					}
					if (this.Columns[field].isShow) {
						tmpCol.style.display = '';
					} else {
						tmpCol.style.display = 'none';
					}
					tmpCol.style.whiteSpace = 'nowrap';
					if (this.cellSelection && this.Columns[field].allowHide) {
						$(this.columnUI).find('input[name=' + _self.id + '_' + this.Columns[field].dataIndex + ']').attr('cells').push(tmpCol);
					}
					tmpRow.appendChild(tmpCol);
				}
				if (this.cellSelection && this.columnUI.cellCount > 0) {
					var tmpTh = document.createElement('td');
					tmpTh.className = 'th01';
					tmpRow.appendChild(tmpTh);
				}
			}
			if (this.pageNavigation) {
				this.setPageConfig();
			}
			if (this.rowSelection) {
				this.selected = {};
				if (!this.singleSelect) document.getElementById(_self.id + "_selAllRow").checked = false;
			}
			var userPageSize = top.$.cookie('UserPageSize');
			if (!userPageSize && this.store.data[this.store.reader.pageSize] && this.pageNavigation) {
				this.store.params.pageSize = parseInt(this.store.data[this.store.reader.pageSize], 10);
				top.$.cookie('UserPageSize', this.store.params.pageSize);
				$('#' + _self.id + 'selResetPageSize').val(this.store.params.pageSize);
			}
		}
	};
	this.GetSelection = function() {
		var arrSel = [];
		var datSource = [];
		if (this.store.reader["jsonRoot"] && this.store.reader['dataRoot']) {
			datSource = eval('this.store.data.' + this.store.reader.dataRoot);
		} else {
			datSource = this.store.data;
		}
		if (!datSource || datSource.length == 0) {
			return arrSel;
		}
		for (var selItem in this.selected) {
			var record = {};
			if (datSource[parseInt(selItem, 10)]) {
				record = datSource[parseInt(selItem, 10)];
			}
			if (arguments.length > 1) {
				var tmpObject = {};
				for (var argItem = 0; argItem < arguments.length; argItem++) {
					if (typeof (record[arguments[argItem]]) != "undefined") {
						tmpObject[arguments[argItem]] = record[arguments[argItem]];
					}
				}
				arrSel.push(tmpObject);
			} else if (arguments.length > 0) {
				arrSel.push(record[arguments[0]]);
			} else {
				arrSel.push(record);
			}
		}
		return arrSel;
	};
	this.GetDataByIndex = function(cellIndex) {
		var arrSel = [];
		var datSource = [];
		if (this.store.reader["jsonRoot"] && this.store.reader['dataRoot']) {
			datSource = eval('this.store.data.' + this.store.reader.dataRoot);
		} else {
			datSource = this.store.data;
		}
		if (!datSource || datSource.length == 0) {
			return arrSel;
		}
		for (var selItem in datSource) {
			if (typeof datSource[selItem][cellIndex] != 'undefined') {
				arrSel.push(datSource[selItem][cellIndex]);
			}
		}
		return arrSel;
	};
	this.GetIndexOf = function(field, value) {
		var datSource = [];
		if (this.store.reader["jsonRoot"] && this.store.reader['dataRoot']) {
			datSource = eval('this.store.data.' + this.store.reader.dataRoot);
		} else {
			datSource = this.store.data;
		}
		if (!datSource || datSource.length == 0) {
			return -1;
		}
		for (var c = 0; c < datSource.length; c++) {
			if (typeof datSource[c][field] != 'undefined' && datSource[c][field] == value) {
				return c;
			}
		}
		return -1;
	};
	this.onDataChanged = function() { };
	this.onFailure = function() { alert('获取数据失败！'); };
	this.store.onSuccess = function() { _self.onSuccess.apply(_self, arguments); };
	this.store.onFailure = function() { _self.onFailure.apply(_self, arguments); };
};