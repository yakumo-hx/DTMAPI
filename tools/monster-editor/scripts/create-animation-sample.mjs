// Original geometric test artwork, deterministically generated from pixel primitives.
// No game sprites or extracted assets. Re-run to recreate the sample and embedded PNG.
import {writeFileSync,mkdirSync} from 'node:fs';
import {deflateSync} from 'node:zlib';
import {fileURLToPath} from 'node:url';
import {createProject,makeScatterSkill,makeSequenceStep} from '../src/model-v2.js';
import {makeActionAnimation} from '../src/action-animation.js';
const width=192,height=32,pixels=Buffer.alloc((width*4+1)*height);
const colors={edge:[24,42,58,255],shell:[91,171,197,255],light:[201,241,245,255],core:[255,175,64,255],fire:[255,241,151,255]};
for(let f=0;f<6;f++)for(let y=0;y<32;y++)for(let x=0;x<32;x++){
 let c=null,dx=x-15.5,dy=y-15.5,r=Math.hypot(dx,dy),charge=[2,4,6,9,5,2][f];
 if(r<12)c=colors.edge;if(r<10)c=colors.shell;
 if(r<8&&y<15)c=colors.light;
 if(Math.abs(dx)<13&&Math.abs(dy)<2)c=colors.edge;
 if(r<charge+1)c=colors.edge;if(r<charge)c=f===3?colors.fire:colors.core;
 if(f===3&&((Math.abs(dy)<1&&Math.abs(dx)>12)||(Math.abs(dx)<1&&Math.abs(dy)>12)))c=colors.fire;
 if(c)for(let k=0;k<4;k++)pixels[y*(width*4+1)+1+(f*32+x)*4+k]=c[k];
}
function crc(buf){let c=0xffffffff;for(const b of buf){c^=b;for(let k=0;k<8;k++)c=(c>>>1)^((c&1)?0xedb88320:0);}return(c^0xffffffff)>>>0;}
function chunk(name,data){const type=Buffer.from(name),n=Buffer.alloc(4),checksum=Buffer.alloc(4);n.writeUInt32BE(data.length);checksum.writeUInt32BE(crc(Buffer.concat([type,data])));return Buffer.concat([n,type,data,checksum]);}
const header=Buffer.alloc(13);header.writeUInt32BE(width);header.writeUInt32BE(height,4);header[8]=8;header[9]=6;
const png=Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',header),chunk('IDAT',deflateSync(pixels)),chunk('IEND',Buffer.alloc(0))]);
const out=fileURLToPath(new URL('../../../author-sdk/examples/monster-action-animation/',import.meta.url));mkdirSync(out,{recursive:true});writeFileSync(out+'action-atlas.png',png);
const p=createProject();p.pack={id:'Samples.MonsterAnimation',name:'单动作动画',author:'DTMAPI',version:'0.1.0'};
p.assets=[{id:'charge',name:'原创充能图集',dataUrl:'data:image/png;base64,'+png.toString('base64'),width,height}];
const m=p.monsters[0];m.id='charge';m.templateId='ball_drone';m.overrides={health:300,display:{name:'动画配置',nameplate:true,healthBar:'Always'}};
const skill=makeScatterSkill('fan','AtkNormal');Object.assign(skill.scatter,{bulletCount:1,bulletSpeed:4,bulletLifetimeGameSeconds:2,spreadDegrees:0,damage:7,range:12,windupGameSeconds:1.2,recoveryGameSeconds:.6,cooldownGameSeconds:5});skill.animation=makeActionAnimation('charge');
m.behavior={mode:'configured-sequence',skills:[skill],sequence:{repeatCount:1,steps:[{...makeSequenceStep('fire','skill'),skillId:'fan'}]}};
writeFileSync(new URL('../samples/action-animation.dtmonster.json',import.meta.url),JSON.stringify(p,null,2)+'\n');
