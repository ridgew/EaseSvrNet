/*****************************************************************************
 * getPlugin - jQuery Plugin for on-demand loading of scripts and styles
 * Documentation : http://nicolas.rudas.info/jQuery/getPlugin/
 * Version: 080405 - 05 April 2008
 ******************************/
// Define your plugins here

WIMP.Plugins = {

    DesktopTabsRender : {_function: 'WIMP.UIHelper.RenderTabs.Render', 
		selectors : ["[id*='right']"], 
		files : [
			{_type : "script", src:'js/AppTabview.js' },
			{_type : "script", src:'js/DesktopParts/RenderTabs.js' }
		]
	}
};


var plugin = WIMP.Plugins;
(function($){
// Set debug to true to show messages in Firebug's console or Safari's javascript console
// Note: showing these messages may make the script slower, so keep this to false when used live
	var debug  = false;
	var plugin_defaults = {
		files : {
			css : {media : "screen",type : "text/css",rel : "stylesheet"},
			script : {type : "text/javascript"}
		}
	};
	var time = 0;
// Update array of plugins with defaults	
	jQuery.each(plugin,function(){
		var p = this;
		jQuery.each(p.files,function(){
			var f = this;f._loaded=false;
			jQuery.extend(f,plugin_defaults.files[f._type]);
		});
	});
	
	
	isNeeded = function(selectors){
		if(window.console && debug) {
			var t = new Date().valueOf();
			console.info('检查依赖资源 ('+selectors.join(', ')+')...');
		}
		var _return = false;
		for (i=0;i<selectors.length;i++){
			if($(selectors[i]).length>0) { _return = true;}
		}
		if(window.console && debug) {
			var t2 = new Date().valueOf();
			time += (t2-t);
			console.info('依赖资源检查完成，耗时 '+(t2-t)+' 毫秒 ('+selectors.join(', ')+') '+ _return);
		}
		return _return;		
	};
	
	whichNeeded = function(){
		var _plugins = [];
		if(window.console && debug) {
			var t = new Date().valueOf();
			console.info('开始检查依赖资源是否匹配...');
		}
		jQuery.each(plugin,function(){
			var plugin = this, selectors = plugin.selectors;
			if (isNeeded(selectors)) { _plugins.push(plugin);  $.getPlugin(this,false); }
		});
		if(window.console && debug) {
			var t2 = new Date().valueOf();
			time += (t2-t);
			console.info('依赖资源检查是否匹配完成，耗时 '+(t2-t)+' 毫秒 ('+_plugins.join(', ')+')');
		}
		return _plugins;
	};
	
	check = function(_options){
		if(window.console && debug) {
			var t = new Date().valueOf();
			console.info('检查函数原型状态 ('+_options._function+')...');
		}
		var chkType;
		try
		{
			chkType = eval("typeof "+_options._function);
		}
		catch (e) { }
		if(chkType && chkType != "undefined") {
			
			for(f=0;f<_options._callbacks.length;f++){
				if(typeof _options._callbacks[f] == "function"){
				 	_options._callbacks[f].apply(_options._data[f]);
				}
				_options._callbacks[f]= "" ;
				_options._data[f] = "";
			}
			
			clearInterval(_options.timer);
			clearTimeout(_options.timer_not_found);
			_options.timer = false;
		}
		if(window.console && debug) {
			var t2 = new Date().valueOf();
			time += (t2-t);
			console.info('函数原型检查(运行)完成，耗时 '+(t2-t)+' 毫秒 ('+_options._function+')');
		}
	};
	
	call = function(_options){
		if(!_options.timer) {
			_options.timer = true;
			
			var interval = 3+Math.random().toString().substring(2,4);
			
			_options.timer = setInterval(function(){
				check(_options);
			},interval);
			
			_options.timer_not_found = setTimeout(function(){
				clearInterval(_options.timer);
				clearTimeout(_options.timer_not_found);
			},4000);
		} else {
			
		}
	};
	
	$.extend($, {
		getPlugin : function(){
			this.getNeeded = function(){return whichNeeded();};
			if(arguments.length == 0){return this;}
			
			var _options = arguments[0];
			if(window.console && debug) {
				var t = new Date().valueOf();
				console.info('加载插件，运行 $.getPlugin ('+_options._function+')...');
			}
		// Defaults
			var _check_attributes = true, _data = false, _callback = false;
			
			
			for(var i =1; i<arguments.length;i++){
				if(typeof arguments[i] == "boolean") {_check_attributes = arguments[i];}
				else if(typeof arguments[i] == "function"){_callback = arguments[i];}
				else {_data = arguments[i];}
			}
			
								
			(typeof _options._callbacks == "object") ? _options._callbacks.push(_callback) : _options._callbacks = [_callback];
			(typeof _options._data == "object") ? _options._data.push(_data) : _options._data = [_data];
						
			if ( (_check_attributes && isNeeded(_options.selectors) ) || !_check_attributes){
				jQuery.each(_options.files,function(){
					var file = this, file_type = file._type; file_element = (file_type=="css") ? "link" : file_type;
					var file_location = (file.src || file.href), file_location_attr = (file.src) ? "src" : "href";
																																						
					if(!_check_attributes || !file._loaded && (_check_attributes && $(file_element+"["+file_location_attr+"='"+file_location+"']").length == 0))  {
						
						if(window.console && debug) {
							var p = new Date().valueOf();
							console.info('导入资源 ('+file_location+')...');
						}
						
						var _load = (document.getElementsByTagName("head")[0] || document.getElementsByTagName("body")[0]);
					//	var _load = document.getElementsByTagName("body")[0];
						_load = _load.appendChild(document.createElement(file_element));
						
						jQuery.each(file, function(a,v){// k = attribute name, v = value
							if(a.toString().substring(0,1) != "_"){
								_load[a] = v.replace(/\//g,"\/");
							}
						});
						
						file._loaded = true;
						if(window.console && debug) {
							var p2 = new Date().valueOf();
							time += (p2-p);
							console.info('导入资源完成，耗时 '+(p2-p)+' 毫秒 ('+file_location+')');
						}
					} else {
					//	console.error('FILE ALREADY FOUND:')
					//	console.error(file_location)
					}
	
				}); // end importing files
				if(_callback){ call(_options) ;}
			} // end check whether to import or not
			if(window.console && debug) {
				var t2 = new Date().valueOf();
				time += (t2-t);
				console.info('插件加载完成 $.getPlugin ('+_options._function+')耗时 '+(t2-t)+' 毫秒' );
			}
		} // end $.getPlugin
	}); // end jQuery extend
	whichNeeded();
	$(document).bind('ready',function(){
		setTimeout(function(){
			if(window.console && debug){console.warn('$.getPlugin 全部插件加载消耗时间: '+time+' 毫秒'); }
			time = 0;
		},5000);
	});
})(jQuery);