/*
* Treeview 1.4 - jQuery plugin to hide and show branches of a tree
* 
* http://bassistance.de/jquery-plugins/jquery-plugin-treeview/
* http://docs.jquery.com/Plugins/Treeview
*
* Copyright (c) 2007 Jörn Zaefferer
*
* Dual licensed under the MIT and GPL licenses:
*   http://www.opensource.org/licenses/mit-license.php
*   http://www.gnu.org/licenses/gpl.html
*
* Revision: $Id: jquery.treeview.js 463 2010-08-12 11:15:07Z wangqj $
*
*/

; (function($) {

	$.extend($.fn, {
		swapClass: function(c1, c2) {
			var c1Elements = this.filter('.' + c1);
			this.filter('.' + c2).removeClass(c2).addClass(c1);
			c1Elements.removeClass(c1).addClass(c2);
			return this;
		},
		replaceClass: function(c1, c2) {
			return this.filter('.' + c1).removeClass(c1).addClass(c2).end();
		},
		hoverClass: function(className) {
			className = className || "hover";
			return this.hover(function() {
				$(this).addClass(className);
			}, function() {
				$(this).removeClass(className);
			});
		},
		heightToggle: function(animated, callback) {
			animated ?
				this.animate({ height: "toggle" }, animated, callback) :
				this.each(function() {
					jQuery(this)[jQuery(this).is(":hidden") ? "show" : "hide"]();
					if (callback)
						callback.apply(this, arguments);
				});
		},
		heightHide: function(animated, callback) {
			if (animated) {
				this.animate({ height: "hide" }, animated, callback);
			} else {
				this.hide();
				if (callback)
					this.each(callback);
			}
		},
		prepareBranches: function(settings) {
			if (!settings.prerendered) {
				// mark last tree items
				this.filter(":last-child:not(ul)").addClass(CLASSES.last);
				// collapse whole tree, or only those marked as closed, anyway except those marked as open
				this.filter((settings.collapsed ? "" : "." + CLASSES.closed) + ":not(." + CLASSES.open + ")").find(">ul").hide();
			}
			// return all items with sublists
			return this.filter(":has(>ul)");
		},
		applyClasses: function(settings, toggler) {
			this.filter(":has(>ul):not(:has(>a))").find(">span").click(function(event) {
				toggler.apply($(this).next());
			}).add($("a", this)).hoverClass();

			if (!settings.prerendered) {
				// handle closed ones first
				this.filter(":has(>ul:hidden)")
						.addClass(CLASSES.expandable)
						.replaceClass(CLASSES.last, CLASSES.lastExpandable);

				// handle open ones
				this.not(":has(>ul:hidden)")
						.addClass(CLASSES.collapsable)
						.replaceClass(CLASSES.last, CLASSES.lastCollapsable);

				// create hitarea
				this.prepend("<div class=\"" + CLASSES.hitarea + "\"/>").find("div." + CLASSES.hitarea).each(function() {
					var classes = "";
					$.each($(this).parent().attr("class").split(" "), function() {
						classes += this + "-hitarea ";
					});
					$(this).addClass(classes);
				});
			}

			// apply event to hitarea
			this.find("div." + CLASSES.hitarea).click(toggler);
		},
		treeview: function(settings) {

			settings = $.extend({
				cookieId: "treeview"
			}, settings);

			if (settings.add) {
				return this.trigger("add", [settings.add]);
			}

			if (settings.toggle) {
				var callback = settings.toggle;
				settings.toggle = function() {
					return callback.apply($(this).parent()[0], arguments);
				};
			}

			// factory for treecontroller
			function treeController(tree, control) {
				// factory for click handlers
				function handler(filter) {
					return function() {
						// reuse toggle event handler, applying the elements to toggle
						// start searching for all hitareas
						toggler.apply($("div." + CLASSES.hitarea, tree).filter(function() {
							// for plain toggle, no filter is provided, otherwise we need to check the parent element
							return filter ? $(this).parent("." + filter).length : true;
						}));
						return false;
					};
				}
				// click on first element to collapse tree
				$("a:eq(0)", control).click(handler(CLASSES.collapsable));
				// click on second to expand tree
				$("a:eq(1)", control).click(handler(CLASSES.expandable));
				// click on third to toggle tree
				$("a:eq(2)", control).click(handler());
			}

			// handle toggle event
			function toggler() {
				$(this)
					.parent()
				// swap classes for hitarea
					.find(">.hitarea")
						.swapClass(CLASSES.collapsableHitarea, CLASSES.expandableHitarea)
						.swapClass(CLASSES.lastCollapsableHitarea, CLASSES.lastExpandableHitarea)
					.end()
				// swap classes for parent li
					.swapClass(CLASSES.collapsable, CLASSES.expandable)
					.swapClass(CLASSES.lastCollapsable, CLASSES.lastExpandable)
				// find child lists
					.find(">ul")
				// toggle them
					.heightToggle(settings.animated, settings.toggle);
				if (settings.unique) {
					$(this).parent()
						.siblings()
					// swap classes for hitarea
						.find(">.hitarea")
							.replaceClass(CLASSES.collapsableHitarea, CLASSES.expandableHitarea)
							.replaceClass(CLASSES.lastCollapsableHitarea, CLASSES.lastExpandableHitarea)
						.end()
						.replaceClass(CLASSES.collapsable, CLASSES.expandable)
						.replaceClass(CLASSES.lastCollapsable, CLASSES.lastExpandable)
						.find(">ul")
						.heightHide(settings.animated, settings.toggle);
				}
			}

			function serialize() {
				function binary(arg) {
					return arg ? 1 : 0;
				}
				var data = [];
				branches.each(function(i, e) {
					data[i] = $(e).is(":has(>ul:visible)") ? 1 : 0;
				});
				$.cookie(settings.cookieId, data.join(""));
			}

			function deserialize() {
				var stored = $.cookie(settings.cookieId);
				if (stored) {
					var data = stored.split("");
					branches.each(function(i, e) {
						$(e).find(">ul")[parseInt(data[i]) ? "show" : "hide"]();
					});
				}
			}

			// add treeview class to activate styles
			this.addClass("treeview");

			// prepare branches and find all tree items with child lists
			var branches = this.find("li").prepareBranches(settings);

			switch (settings.persist) {
				case "cookie":
					var toggleCallback = settings.toggle;
					settings.toggle = function() {
						serialize();
						if (toggleCallback) {
							toggleCallback.apply(this, arguments);
						}
					};
					deserialize();
					break;
				case "location":
					var current = this.find("a").filter(function() { return this.href.toLowerCase() == location.href.toLowerCase(); });
					if (current.length) {
						current.addClass("selected").parents("ul, li").add(current.next()).show();
					}
					break;
			}

			branches.applyClasses(settings, toggler);

			// if control option is set, create the treecontroller and show it
			if (settings.control) {
				treeController(this, settings.control);
				$(settings.control).show();
			}

			return this.bind("add", function(event, branches) {
				$(branches).prev()
					.removeClass(CLASSES.last)
					.removeClass(CLASSES.lastCollapsable)
					.removeClass(CLASSES.lastExpandable)
				.find(">.hitarea")
					.removeClass(CLASSES.lastCollapsableHitarea)
					.removeClass(CLASSES.lastExpandableHitarea);
				$(branches).find("li").andSelf().prepareBranches(settings).applyClasses(settings, toggler);
			});
		}
	});

	// classes used by the plugin
	// need to be styled via external stylesheet, see first example
	var CLASSES = $.fn.treeview.classes = {
		open: "open",
		closed: "closed",
		expandable: "expandable",
		expandableHitarea: "expandable-hitarea",
		lastExpandableHitarea: "lastExpandable-hitarea",
		collapsable: "collapsable",
		collapsableHitarea: "collapsable-hitarea",
		lastCollapsableHitarea: "lastCollapsable-hitarea",
		lastCollapsable: "lastCollapsable",
		lastExpandable: "lastExpandable",
		last: "last",
		hitarea: "hitarea"
	};

	// provide backwards compability
	$.fn.Treeview = $.fn.treeview;

})(jQuery);



function getTree(strTitle, jQueryUL, strProtocol, strPostData, jsonConfig, strRightCode) {
	var fnGetTree = arguments[6];
	var fnClick = arguments[7];
	var objImg = arguments[8];
	jQueryUL.html('<li class="last">数据加载中...</li>');
	$.ajax({
		type: 'POST',
		url: '../service/' + strProtocol,
		data: strPostData,
		contentType: "application/json; charset=utf-8",
		dataType: "json",
		success: function(response) {
			json = response.d;
			jQueryUL.html('');
			for (var i = 0; i < json.Data.length; i++) {
				if (typeof (objImg) == 'function' || typeof (objImg) == 'object') {
					var isObjectBind = (typeof (objImg) == 'object');
					var nid = 'li' + strProtocol + '_' + json.Data[i][jsonConfig.ID];
					var objImgResult = (isObjectBind && objImg.renderObject) ? objImg.renderObject.call(json.Data[i]) : objImg.call(json.Data[i]);
					if (typeof (objImgResult) == 'object') {
						var objLi = $('<li id="' + nid + '" title="' + strTitle + '" protocol="' + json.Protocol + '"' + ' value="' + json.Data[i][jsonConfig.ID] + '"></li>');
						objImgResult.appendTo(objLi);
						if (json.Data[i][jsonConfig.hasChild]) $('<ul></ul>').appendTo(objLi);
						objLi.appendTo(jQueryUL);
					}
					else if (typeof (objImgResult) == 'string') {
						jQueryUL.append('<li id="' + nid + '" title="' + strTitle + '" protocol="' + json.Protocol + '"' + ' value="' + json.Data[i][jsonConfig.ID] + '">' + objImgResult + (json.Data[i][jsonConfig.hasChild] ? '<ul></ul>' : '') + '</li>');
					}
					//自定义页面数据绑定
					if (objImg.renderBind) objImg.renderBind(jQueryUL, $('#' + nid), json.Data[i]);
				}
				else if (typeof (objImg) == 'string') {
					jQueryUL.append('<li id="li' + strProtocol + '_' + json.Data[i][jsonConfig.ID] + '" title="' + strTitle + '" protocol="' + json.Protocol + '"' + ' value="' + json.Data[i][jsonConfig.ID] + '">' + objImg + (json.Data[i][jsonConfig.hasChild] ? '<ul></ul>' : '') + '</li>');
				}
				else {
					jQueryUL.append('<li id="li' + strProtocol + '_' + json.Data[i][jsonConfig.ID] + '" title="' + strTitle + '" protocol="' + json.Protocol + '"' + ' value="' + json.Data[i][jsonConfig.ID] + '">' + '<div class="title"><img border="0" align="middle" src="../images/type.gif"/><span>' + json.Data[i][jsonConfig.Name] + '</span></div>' + (json.Data[i][jsonConfig.hasChild] ? '<ul></ul>' : '') + '</li>');
				}
			}
			jQueryUL.find('>li').addClass('closed');
			jQueryUL.find('div.title').mousedown(function() {
				function getRootNode(obj) {
					if (obj.parentNode && obj.parentNode.id != 'tree')
						return getRootNode(obj.parentNode);
					else
						return obj.parentNode;
				}
				$(getRootNode(this)).find('div.title').css({ 'backgroundColor': '', 'color': '#000000' });
				top.window.___CurrentTreeNodeParent = $(this).css({ 'backgroundColor': '#2682C6', 'color': '#ffffff' }).parent().parent().parent();
			})
            .click(function() { if (typeof (fnClick) == 'function') fnClick.call(this.parentNode); });
			jQueryUL.treeview({
				toggle: function() {
					var jObj = $(this);
					if (jObj.hasClass('collapsable') && typeof (fnGetTree) == 'function') {
						if (jObj.find('>ul.treeview').length < 1 || jObj.hasClass('refresh')) {
							fnGetTree.call(this);
							if (jObj.hasClass('refresh')) { jObj.removeClass('refresh'); }
						}
					}
				}
			});
			window.top.init_contentMenu(jQueryUL, strRightCode);
		},
		error: function() {
			alert('数据加载出错！请检查网络和服务器状态！');
		}
	});
}

function RefreshTreeNode() {
	var jNode;
	if (arguments.length > 0) {
		jNode = arguments[0].jquery ? arguments[0] : $(arguments[0]);
	} else if (top.window.___CurrentTreeNodeParent.jquery) {
		jNode = top.window.___CurrentTreeNodeParent;
	} else {
		window.location.reload();
	}
	if (jNode && jNode.jquery && (jNode.hasClass('collapsable') || jNode.hasClass('expandable'))) {
		jNode.addClass('refresh');
		jNode.find('div:first').click().click();
	} else {
	    window.location.reload();
	}	
}