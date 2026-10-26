'use strict';
const fs = require('fs');
const vm = require('vm');
const assert = require('node:assert/strict');
const path = require('node:path');
const prefix = 'localgpt';
const file = path.join(__dirname, '../..', 'docs/templates/localgpt/public/main.js');
{
 const source = fs.readFileSync(file,'utf8');
 const code = source.slice(source.indexOf('function clippedMermaidNodeLabels(block) {'), source.indexOf('async function recoverMermaidDiagrams() {'));
 assert.ok(code.startsWith('function clippedMermaidNodeLabels'));
 const label = {style:{}, getBoundingClientRect() {const factor=Number.parseFloat(this.style.fontSize||'16')/16; return {left:3,right:3+112*factor,top:12,bottom:12+18*factor,width:112*factor,height:18*factor};}};
 const shape = {getBoundingClientRect(){return {left:0,right:100,top:0,bottom:45,width:100,height:45}}};
 const node = {querySelector(s){if(s.includes('rect'))return shape;if(s==='text' || s.includes('.label'))return label;return null;}};
 const svg = {querySelectorAll(s){return s==='.node'?[node]:[]}};
 const diagram = {querySelector(s){return s==='svg'?svg:null;}};
 const context={window:{getComputedStyle(){return {fontSize:'16px'}}},console};
 vm.createContext(context);vm.runInContext(code,context);
 assert.equal(context.hasClippedMermaidLabels(diagram),true, prefix+' initial overflow');
 assert.equal(context.fitAlreadyRenderedMermaidLabels(diagram),true, prefix+' fit');
 assert.equal(context.hasClippedMermaidLabels(diagram),false,prefix+' after fit');

 const start = source.indexOf('function installKawaiiTouchScratchToggle() {');
 const end = source.indexOf('function installKawaiiTearInteraction() {');
 assert.ok(start>=0 && end>start);
 const classes = new Set();const listeners={};let button;
 const ctx={
  window:{matchMedia(){return {matches:true}}},
  prefersReducedMotion(){return false},
  document:{documentElement:{classList:{contains:x=>classes.has(x),toggle(x){if(classes.has(x)){classes.delete(x);return false}classes.add(x);return true}}},
   querySelector(){return button||null},createElement(tag){assert.equal(tag,'button');return {setAttribute(n,v){this[n]=v},addEventListener(n,fn){listeners[n]=fn},type:'',className:'',textContent:''}},body:{appendChild(x){button=x}}},
  kawaiiTearSession:null,removeKawaiiTouchPaw(){}
 };
 vm.createContext(ctx);
 vm.runInContext(source.slice(start,end),ctx);
 ctx.installKawaiiTouchScratchToggle();
 assert.ok(button, 'toggle exists');assert.equal(button['aria-pressed'],'false');
 listeners.click();assert.equal(button['aria-pressed'],'true');assert.ok(classes.has(prefix+'-touch-scratch-active'));
 listeners.click();assert.equal(button['aria-pressed'],'false');assert.ok(!classes.has(prefix+'-touch-scratch-active'));
 console.log(prefix+': geometry detection, label rescue, mobile mode toggling passed');
}
