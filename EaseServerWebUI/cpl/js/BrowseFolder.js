var oEditor = window.parent.InnerDialogLoaded ? window.parent.InnerDialogLoaded() : null;
var FCKLang = oEditor ? oEditor.FCKLang : null;
var swfu;
jQuery(function($) {
	RefreshCurrent();
	SetButton('btnNewFolder', 'off');
	$('#btnNewFolder').click(function() {
		ShowLayer("请输入新目录名称", '', function(value) {
			value = value.val().trim();
			if (value.length > 0) {
				CloseLayer('xDialog');
				PageMethods.CreateDirectory(encodeURIComponent(value), function(result) {
					try {
						if (result.success) {
							alert("创建目录成功！");
							ReoadFile(result.data, result.current);
						} else {
							alert("创建目录失败！" + "\n原因：" + result.errors.reason);
						}
					} catch (ex) {
						alert("返回类型错误！请联系管理员。");
					}

				}, ShowError);
			} else {
				alert('请输入新目录名称');
			}
			return false;
		});
	});
	$('#ifUpfile').attr('src', 'javascript:void(0);');
	if (window.downable) {
		$('#btnDownload').show();
		SetButton('btnDownload', 'off');
		$('#btnDownload').click(function() {
			if (window.FileCount > 0) {
				$('#ifUpfile')[0].contentWindow.location = 'BrowseFolder.aspx?act=download';
			} else {
				alert('该目录没有文件可下载');
			}
		});
	}
	SetButton('btnUploadFile', 'off');
	$('#btnUploadFile').click(function() {
		jQuery('#pnlFlashUpload').css('margin-top', 0);
		jQuery('#pnlFlashUpload').css('margin-left', 0);
		var lHeight = jQuery('#pnlFlashUpload').outerHeight(true);
		var lWidth = jQuery('#pnlFlashUpload').outerWidth(true);
		jQuery('#pnlFlashUpload').css('margin-left', -1 * lWidth / 2);
		jQuery('#pnlFlashUpload').css('margin-top', -1 * lHeight / 2);
		$("#loading-mask").show();
		$('#pnlFlashUpload').show();
	});
	InitFlashUploadPanel();
	//refresh
	SetButton('btnRefresh', 'off');
	$('#btnRefresh').click(RefreshCurrent);

	$('#xDialogButtonCancel').click(function() { CloseLayer('xDialog'); });

	//页面加载完成
	if (oEditor) {
		oEditor.FCKLanguageManager.TranslatePage(document);
		window.parent.SetAutoSize(true);
		window.parent.SetOkButton(true);
		window.Ok = function() {
			if ($('#txtCurrent').attr('isfolder') == 'false') {
				if (typeof (window.parent.Args().CustomValue) == 'function')
					window.parent.Args().CustomValue($('#txtCurrent').val());
				return true;
			} else {
				return false;
			}
		};
	}
	window.onunload = function() {
		Sys.WebForms.PageRequestManager.getInstance().abortPostBack();
	};
});


function InitFlashUploadPanel() {
	try {
		var swfSettings = {
			// Backend Settings
			upload_url: SWFUpload.completeURL('BrowseFolder.aspx?act=upfile&flash=true'), // Relative to the SWF file /upfile.aspx;window.location.pathname
			// File Upload Settings
			file_size_limit: window['file_size_limit'],
			file_types: window['file_types'],
			file_types_description: window['file_types_description'],
			file_upload_limit: 100,
			file_queue_limit: 0,

			// event handler functions
			file_queued_handler: FlashUploadEventHandler.fileQueued,
			file_queue_error_handler: FlashUploadEventHandler.fileQueueError,
			upload_start_handler: FlashUploadEventHandler.uploadStart,
			upload_progress_handler: FlashUploadEventHandler.uploadProgress,
			upload_error_handler: FlashUploadEventHandler.uploadError,
			upload_success_handler: FlashUploadEventHandler.uploadSuccess,
			upload_complete_handler: FlashUploadEventHandler.uploadComplete,

			// Button settings
			button_image_url: SWFUpload.completeURL('images/toolbar/Button160_22.png'), // Relative to the SWF file
			button_placeholder_id: "pnlButtonPlaceHolder",
			button_width: 160,
			button_height: 22,
			button_text: '<span class="button">选择你要上传的文件</span>',
			button_text_style: '.button { font-family: Helvetica, Arial, 宋体; font-size: 12px;text-align:center; }',
			button_text_top_padding: 1,
			button_text_left_padding: 1,

			// Flash Settings
			flash_url: SWFUpload.completeURL('js/swfupload.swf'), // Relative to this file

			custom_settings: {
				progressTarget: "fsUploadProgress",
				cancelButtonId: "btnCancelUpload"
			},

			// Debug Settings
			debug: false
		};
		swfu = new SWFUpload(swfSettings);
		$('#btnStartUploadFile').click(function(e) {
			if (swfu.getStats().files_queued > 0) {
				swfu.refreshCookies(false);
				swfu.addPostParam("chkCover", $('#chkCover')[0].checked ? "on" : "off");
				swfu.addPostParam("chkUnzip", $('#chkUnzip')[0].checked ? "on" : "off");
				swfu.addPostParam("ASPSESSID", window['ASPSESSID']);
				swfu.addPostParam("AUTHID", window['AUTHID']);
				//console.log(swfu.settings.post_params);
				swfu.startUpload();
			} else {
				alert('请选择你要上传的文件');
			}
			return false;
		});
		$('#btnCancelUpload').click(function(e) {
			var upstats = swfu.getStats();
			//alert($.JSON.encode(upstats));
			if (swfu.getStats().successful_uploads === 0) {
				CloseLayer('pnlFlashUpload');
			}
			swfu.cancelQueue();
		});
	} catch (ex) {
		alert(ex.message);
	}
}

function RemoveMask() {
	setTimeout(function() {
		$("#loading-mask").fadeOut("slow");
		$("#loading").fadeOut("slow");
	}, 10);
}
function ShowLayer(title, defaltvalue, callback, html) {
	try {
		$('#xDialogTitle').html(title);
		$('#xDialogButtonOk')[0].onclick = function() {
			var value = $('#xDialogContent input');
			if ($.isFunction(callback)) {
				if (callback(value)) {
					CloseLayer('xDialog');
				}
			}
		};
		if (html) {
			$('#xDialogContent').html(html);
		} else {
			$('#xDialogContent').html('<input type="text" id="xDialogField" />');
		}
		$('#xDialogContent input').val(defaltvalue);
		jQuery('#xDialog').css('margin-top', 0);
		jQuery('#xDialog').css('margin-left', 0);
		var lHeight = jQuery('#xDialog').outerHeight(true);
		jQuery('#xDialog').css('margin-top', -1 * lHeight / 2);
		var lWidth = jQuery('#xDialog').outerWidth(true);
		jQuery('#xDialog').css('margin-left', -1 * lWidth / 2);

		$("#loading-mask").show();
		$('#xDialog').show();
		$('#xDialogContent input').focus();
	} catch (ex) {
		alert(ex.message);
	}
}

function CloseLayer(id) {
	try {
		$("#loading-mask").hide();
		$('#' + id).hide();
	} catch (ex) {
		alert(ex.message);
	}
}

function SetButton(id, model) {
	if ($('#' + id).length > 0) {
		switch (model) {
			case 'on':
				$('#' + id)[0].className = 'TB_Button_On';
				$('#' + id).hover(function() { this.className = 'TB_Button_On_Over'; }, function() { this.className = 'TB_Button_On'; });
				break;
			case 'off':
				$('#' + id)[0].className = 'TB_Button_Off';
				$('#' + id).hover(function() { this.className = 'TB_Button_Off_Over'; }, function() { this.className = 'TB_Button_Off'; });
				break;
			case 'disable':
				$('#' + id).unbind();
				$('#' + id)[0].className = 'TB_Button_Disabled';
				break;
		}
	}
}
function FitImageSize() {
	var __parent = $(this).parent();
	var MAX_WIDTH = __parent.width();
	var MAX_HEIGHT = __parent.height();
	var IMG_MARGIN = 0;
	var tImg = new Image();
	var obj = this;
	tImg.onload = function() {
		var w = this.width;
		var h = this.height;
		var tw = 0, th = 0;
		if (w <= MAX_WIDTH && h <= MAX_HEIGHT) {
			tw = w;
			th = h;
		} else {
			if (w > h) {
				tw = MAX_WIDTH;
				th = MAX_WIDTH * h / w;
			} else {
				th = MAX_HEIGHT;
				tw = MAX_HEIGHT * w / h;
			}
		}
		obj.width = tw;
		obj.height = th;
		obj.style.marginLeft = IMG_MARGIN + (MAX_WIDTH - tw) / 2 + 'px';
		obj.style.marginTop = IMG_MARGIN + (MAX_HEIGHT - th) / 2 + 'px';
		$(obj).show();
	}
	tImg.src = obj.src;
}

function PreviewImage() {
	var _parent = $(this).parent().parent();
	var path = _parent.attr('path');
	var isFolder = _parent.attr('isfolder');
	isFolder = isFolder == 'true' ? true : false;
	var previewDiv = $('#pnlPreviewPicture');
	var extname = path.substr(path.lastIndexOf(".") + 1);
	if (!isFolder && previewDiv.css('display') == 'none' && (extname == "gif" || extname == "bmp" || extname == "jpg" || extname == "jpeg" || extname == "png")) {
		var documentw = $(document.body).width();
		var documenth = $(document.body).height();
		previewDiv.empty();
		previewDiv.css('left', 0).css('top', 0);
		var pos = $(this).offset();
		var img = $('<img src="' + path + '">');
		img.hide();
		img[0].onload = FitImageSize;
		var twidth = $(this).width();
		var theight = $(this).height();
		var realHeight = previewDiv.outerHeight(true);
		var realWidth = previewDiv.outerWidth(true);
		pos.left += twidth + 20;
		pos.top -= previewDiv.height() / 2;

		pos.left = pos.left + realWidth > documentw ? documentw - realWidth : pos.left;
		pos.top = pos.top + realHeight > documenth ? documenth - realHeight : pos.top;
		pos.left = pos.left < 0 ? 0 : pos.left;
		pos.top = pos.top < 0 ? 0 : pos.top;

		previewDiv.css('left', pos.left).css('top', pos.top).append(img);
		previewDiv.show();
	} else {
		previewDiv.hide();
	}
}

function ReName() {
	var _parent = $(this).parent();
	var isFolder = _parent.attr('isfolder');
	isFolder = isFolder == 'true' ? true : false;
	var name = _parent.attr('name').trim();
	var mainName = name;
	if (!isFolder) {
		mainName = name.substr(0, name.lastIndexOf('.'));
	}
	ShowLayer('请输入新文件名', mainName, function(value) {
		value = value.val().trim();
		if (value.length > 0) {
			CloseLayer('xDialog');
			PageMethods.Rename(encodeURIComponent(name), encodeURIComponent(value), isFolder, function(result) {
				try {
					if (result.success) {
						ReoadFile(result.data, result.current);
						alert("修改名称成功！");
					} else {
						alert("修改名称失败！" + "\n原因：" + result.errors.reason);
						RefreshCurrent();
					}
				} catch (ex) {
					alert("返回类型错误！请联系管理员。");
				}
			}, ShowError);
		} else {
			alert("请输入新文件名！");
		}
		return false;
	});
}

function Down() {
	var _parent = $(this).parent();
	var isFolder = _parent.attr('isfolder');
	isFolder = isFolder == 'true' ? true : false;
	var name = _parent.attr('name').trim();
	if (name.length > 0) {
		$('#ifUpfile')[0].contentWindow.location = 'BrowseFolder.aspx?act=download&Name=' + name + '&isFolder=' + isFolder;
	} else {
		alert('没有文件可下载');
	}
}

function Delete() {
	var _parent = $(this).parent();
	var isFolder = _parent.attr('isfolder');
	isFolder = isFolder == 'true' ? true : false;
	if (confirm("此操作将不可恢复。\n确定要删除该" + (isFolder ? "目录及所有子目录" : "文件") + "吗？")) {
		PageMethods.Delete(encodeURIComponent(_parent.attr('name')), isFolder, function(result, context) {
			try {
				if (result.success) {
					if (result.total < 1) {
						$(context).parent().parent().parent().parent().parent().html('<li class="out">该目录没有文件</li>');
					} else {
						$(context).parent().parent().parent().parent().remove();
					}
					alert(isFolder ? "删除目录成功！" : "删除文件成功！");
				} else {
					alert(isFolder ? "删除目录失败！" : "删除文件失败！" + "\n原因：" + result.errors.reason);
					RefreshCurrent();
				}
			} catch (ex) {
				alert("返回类型错误！请联系管理员。");
			}

		}, ShowError, this);
	}
}

function Select(event) {
	if (event && event.preventDefault) {
		event.preventDefault();
	}
	var _parent = $(this).parent().parent();
	var isFolder = _parent.attr('isfolder');
	isFolder = isFolder == 'true' ? true : false;
	if (isFolder) {
		PageMethods.EnterChild(_parent.attr('name'), function(result) {
			try {
				if (result.success) {
					if (typeof result.isRoot != 'undefined') {
						window['isRoot'] = result.isRoot;
					}
					ReoadFile(result.data, result.current);
				} else {
					alert("浏览目录失败！" + "\n原因：" + result.errors.reason);
				}
			} catch (ex) {
				alert("返回类型错误！请联系管理员。");
			}
		}, ShowError);
	} else {
		var __path = _parent.attr('path');
		$('#txtCurrent').attr('isfolder', 'false');
		$('#txtCurrent').val(__path);
		ReturnFile(__path);
	}
}

function ReturnFile(value) {
	try {
		if (value) {
			if (window.opener && window.opener.SetUrl) {
				window.opener.SetUrl(value);
			}
			if (window.frameElement && window.frameElement.callback) {
				window.frameElement.callback(value);
			} else {
				window.returnValue = value;
			}
		}
		if (window.frameElement) {
			if (window.frameElement.id == 'tmp_opt_iframe') {
				try {
					window.parent.fn_del_div();
				} catch (ex) {
				}
			}
			if (window.frameElement.id == 'tmp_opt_iframe1') {
				try {
					window.parent.fn_del_div1();
				} catch (ex) {
				}
			}
			if (window.parent != window && window.parent.Ok) {
				window.parent.Ok();
			}
		} else {
			window.close();
		}
	} catch (ex) {
		Sys.Debug.trace(ex);
	}
}

function OnUploadCompleted(error, path, name, message) {
	RemoveMask();
	if (!error) {
		alert(message);
		RefreshCurrent();
	} else {
		alert(message);
	}
}

function ShowError(error) {
	alert(error.get_message());
}

function ReoadFile(data, current) {
	$('#pnlPreviewPicture').hide();
	$('#txtCurrent').val(current);
	$('#lblCurrent').html(current);
	var ulobj = $('ul');
	window.FileCount = data.length;
	var arrHtml = [];
	for (var i = 0; i < data.length; i++) {
		arrHtml.push('<li class="out" onmouseout="this.className=\'out\'" onmouseover="this.className=\'over\'">\
		<table border="0" cellpadding="0" cellspacing="0" width="100%">\
		<tr name="' + data[i].Name + '" path="' + data[i].Path + '" isfolder="' + data[i].IsFolder + '">\
			<td class="btnPointer" align="center" valign="middle" onclick="Delete.call(this);">\
				<img alt="" title="删除" src="images/toolbar/del.gif" />\
			</td>\
			<td class="btnPointer" align="center" valign="middle" onclick="ReName.call(this);">\
				<img alt="" title="改名" src="images/toolbar/Edit.gif" />\
			</td>'
			+ (window.downable ? '<td class="btnPointer" align="center" valign="middle" onclick="Down.call(this);"><img alt="" title="下载" src="images/toolbar/folder_go.gif" /></td>' : '') +
			'<td style="width:22px;" align="center" valign="middle">\
				<img src="' + (data[i].IsFolder ? 'images/toolbar/folder.gif' : 'images/toolbar/icons/16/file.gif') + '" alt="" />\
			</td>\
			<td valign="middle">\
				<a href="#" onclick="Select.apply(this,arguments);return false;" ondblclick="Select.apply(this,arguments);return false;" title="' + (data[i].IsFolder ? '点击进入目录' : '') + '" onmouseover="PreviewImage.call(this);" onmouseout="PreviewImage.call(this);">' + data[i].Name + ' ' + data[i].Size + '</a>\
			</td>\
			<td style="width:120px;">' + data[i].Date + '</td>\
		</tr>\
		</table>\
		</li>');
	}
	if (arrHtml.length > 0) {
		ulobj.html(arrHtml.join(''));
	} else {
		ulobj.html('<li class="out">该目录没有文件</li>');
	}
	ResetUI();
}
function RefreshCurrent() {
	$('#pnlPreviewPicture').hide();
	PageMethods.RefreshPath(function(result) {
		try {
			if (result.success) {
				ReoadFile(result.data, result.current);
			} else {
				alert("浏览目录失败！" + "\n原因：" + result.errors.reason);
			}
		} catch (ex) {
			alert("返回类型错误！请联系管理员。" + ex.description);
		}
	}, ShowError);
}

function ResetUI() {
	$('.fileList').height('auto');
	$('.fileList').css('overflow', 'auto');
	var wHeight = $(window).height();
	var bHeight = $(document.body).outerHeight(true);
	if (bHeight != document.body.scrollHeight) {
		bHeight = document.body.scrollHeight;
	}
	if (wHeight < bHeight) {
		$('.fileList').height($('.fileList').height() + (wHeight - bHeight));
		$('.fileList').css('overflow', 'hidden');
		$('.fileList').css('overflow-x', 'hidden');
		$('.fileList').css('overflow-y', 'auto');
	}
	$('#txtCurrent').attr('isfolder', 'true');
	if (!window['isRoot']) {
		$('#btnUpFolder').unbind();
		$('#btnUpFolder').attr('title', '返回上级目录');
		SetButton('btnUpFolder', 'off');
		$('#btnUpFolder').click(GetParentPath);
	} else {
		SetButton('btnUpFolder', 'disable');
		$('#btnUpFolder').attr('title', '已经是根目录');
	}
}
function GetParentPath() {
	PageMethods.EnterParent(function(result) {
		try {
			if (result.success) {
				if (typeof result.isRoot != 'undefined') {
					window['isRoot'] = result.isRoot;
				}
				ReoadFile(result.data, result.current);
			} else {
				alert("返回上级目录失败！" + "\n原因：" + result.errors.reason);
			}
		} catch (ex) {
			alert("返回类型错误！请联系管理员。" + ex.description);
		}

	}, ShowError);
}

var FlashUploadEventHandler = {
	fileQueued: function(file) {
		try {
			var progress = new FileProgress(file, this.customSettings.progressTarget);
			progress.setStatus("等待...");
			progress.toggleCancel(true, this);
		} catch (ex) {
			this.debug(ex);
		}
	},
	fileQueueError: function(file, errorCode, message) {
		try {
			if (errorCode === SWFUpload.QUEUE_ERROR.QUEUE_LIMIT_EXCEEDED) {
				alert("You have attempted to queue too many files.\n" + (message === 0 ? "You have reached the upload limit." : "You may select " + (message > 1 ? "up to " + message + " files." : "one file.")));
				return;
			}

			var progress = new FileProgress(file, this.customSettings.progressTarget);
			progress.setError();
			progress.toggleCancel(false);

			switch (errorCode) {
				case SWFUpload.QUEUE_ERROR.FILE_EXCEEDS_SIZE_LIMIT:
					progress.setStatus("File is too big.");
					this.debug("Error Code: File too big, File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
				case SWFUpload.QUEUE_ERROR.ZERO_BYTE_FILE:
					progress.setStatus("Cannot upload Zero Byte files.");
					this.debug("Error Code: Zero byte file, File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
				case SWFUpload.QUEUE_ERROR.INVALID_FILETYPE:
					progress.setStatus("Invalid File Type.");
					this.debug("Error Code: Invalid File Type, File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
				default:
					if (file !== null) {
						progress.setStatus("Unhandled Error");
					}
					this.debug("Error Code: " + errorCode + ", File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
			}
		} catch (ex) {
			this.debug(ex);
		}
	},
	uploadStart: function(file) {
		//alert($.JSON.encode(this.getStats()));
		try {
			var progress = new FileProgress(file, this.customSettings.progressTarget);
			progress.setStatus("上传中...");
			progress.toggleCancel(true, this);
			document.getElementById('btnStartUploadFile').disabled = true;
		}
		catch (ex) { }

		return true;
	},
	uploadProgress: function(file, bytesLoaded, bytesTotal) {
		try {
			var percent = Math.ceil((bytesLoaded / bytesTotal) * 100);

			var progress = new FileProgress(file, this.customSettings.progressTarget);
			progress.setProgress(percent);
			progress.setStatus("上传中...");
		} catch (ex) {
			this.debug(ex);
		}
	},
	uploadSuccess: function(file, serverData) {
		try {
			if (serverData) {
				var progress = new FileProgress(file, this.customSettings.progressTarget);
				var info = serverData.split('|');
				if (info[0] && info[0] == 'true') {
					progress.setComplete();
					progress.setStatus("完成.");
				} else {
					progress.setError();
					progress.setStatus("失败.");
					alert("文件" + file.name + " 上传失败\n原因:" + info[1] + "\n请稍后重试！");
				}
				progress.toggleCancel(false);
			}
		} catch (ex) {
			this.debug(ex);
		}
	},
	uploadComplete: function(file) {
		var stats = this.getStats();
		var status = document.getElementById("pnlUploadStatus");
		status.innerHTML = "已成功上传" + stats.successful_uploads + "个文件";
		if (stats.files_queued === 0) {
			alert("已成功上传" + stats.successful_uploads + "个文件");
			try {
				this.setStats({ upload_cancelled: 0, upload_errors: 0, successful_uploads: 0, files_queued: 0, queue_errors: 0, in_progress: 0 });
				document.getElementById('btnStartUploadFile').disabled = false;
				CloseLayer('pnlFlashUpload');
				RefreshCurrent();
			} catch (ex) { alert(ex.message); }
		}
	},
	uploadError: function(file, errorCode, message) {
		try {
			var progress = new FileProgress(file, this.customSettings.progressTarget);
			progress.setError();
			progress.toggleCancel(false);

			switch (errorCode) {
				case SWFUpload.UPLOAD_ERROR.HTTP_ERROR:
					progress.setStatus("Upload Error: " + message);
					this.debug("Error Code: HTTP Error, File name: " + file.name + ", Message: " + message);
					break;
				case SWFUpload.UPLOAD_ERROR.UPLOAD_FAILED:
					progress.setStatus("Upload Failed.");
					this.debug("Error Code: Upload Failed, File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
				case SWFUpload.UPLOAD_ERROR.IO_ERROR:
					progress.setStatus("Server (IO) Error");
					this.debug("Error Code: IO Error, File name: " + file.name + ", Message: " + message);
					break;
				case SWFUpload.UPLOAD_ERROR.SECURITY_ERROR:
					progress.setStatus("Security Error");
					this.debug("Error Code: Security Error, File name: " + file.name + ", Message: " + message);
					break;
				case SWFUpload.UPLOAD_ERROR.UPLOAD_LIMIT_EXCEEDED:
					progress.setStatus("Upload limit exceeded.");
					this.debug("Error Code: Upload Limit Exceeded, File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
				case SWFUpload.UPLOAD_ERROR.FILE_VALIDATION_FAILED:
					progress.setStatus("Failed Validation.  Upload skipped.");
					this.debug("Error Code: File Validation Failed, File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
				case SWFUpload.UPLOAD_ERROR.FILE_CANCELLED:
					progress.setStatus("Cancelled");
					progress.setCancelled();
					break;
				case SWFUpload.UPLOAD_ERROR.UPLOAD_STOPPED:
					progress.setStatus("Stopped");
					break;
				default:
					progress.setStatus("Unhandled Error: " + errorCode);
					this.debug("Error Code: " + errorCode + ", File name: " + file.name + ", File size: " + file.size + ", Message: " + message);
					break;
			}
		} catch (ex) {
			this.debug(ex);
		}
	}
};