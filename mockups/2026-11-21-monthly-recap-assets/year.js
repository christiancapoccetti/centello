const Y={
 year:2026,total:'17.955',count:1143,avg:'1.496',
 prev:{year:2025,total:'18.340',diff:'385',pct:'2%'},
 months:{labels:['G','F','M','A','M','G','L','A','S','O','N','D'],
  names:['gennaio','febbraio','marzo','aprile','maggio','giugno','luglio','agosto','settembre','ottobre','novembre','dicembre'],
  v:[1512,1198,1420,1355,1476,1390,1702,1948,1388,1397,1285,1884]},
 record:{label:'Sabato 8 agosto',total:'412,80',items:[
  {c:'sky',g:'directions_boat',name:'Trasporti',note:'Traghetto per l\'Elba',amt:'286,00'},
  {c:'butter',g:'restaurant',name:'Ristoranti',note:'Cena al porto',amt:'84,50'},
  {c:'mint',g:'shopping_cart',name:'Spesa',note:'Spesa per la casa al mare',amt:'42,30'}]},
 week:{labels:['Lun','Mar','Mer','Gio','Ven','Sab','Dom'],v:[11,12,12,13,16,24,12],top:5},
 cats:[
  {c:'peri',g:'home',name:'Casa',amt:'3.840',v:3840,share:'21%'},
  {c:'mint',g:'shopping_cart',name:'Spesa',amt:'3.610',v:3610,share:'20%'},
  {c:'butter',g:'restaurant',name:'Ristoranti',amt:'2.180',v:2180,share:'12%'},
  {c:'sky',g:'directions_bus',name:'Trasporti',amt:'1.960',v:1960,share:'11%'},
  {c:'lilac',g:'sports_esports',name:'Svago',amt:'1.420',v:1420,share:'8%'},
  {c:'coral',g:'local_cafe',name:'Bar',amt:'690',v:690,share:'4%'},
  {c:'grey',g:'more_horiz',name:'Altre 4 categorie',amt:'4.255',v:4255,share:'24%'}],
 up:{c:'butter',g:'restaurant',name:'Ristoranti',diff:'620',from:'1.560',to:'2.180'},
 down:{c:'sky',g:'directions_bus',name:'Trasporti',diff:'540',from:'2.500',to:'1.960'},
 tag:{c:'butter',g:'local_pizza',cat:'Ristoranti',tag:'Pizza',times:41,amt:'512,40',avg:'12,50',perMonth:[3,4,2,3,5,4,3,2,4,3,4,4]},
 steady:{c:'coral',g:'local_cafe',cat:'Bar',tag:'Caffè',weeks:48,of:52,times:262,missing:[2,32,33,52]},
 fresh:[{c:'lilac',g:'pool',tag:'Piscina',cat:'Svago',since:'da settembre',times:18,amt:'153,00'},
  {c:'lilac',g:'menu_book',tag:'Libri',cat:'Svago',since:'da marzo',times:9,amt:'146,70'},
  {c:'mint',g:'pets',tag:'Veterinario',cat:'Spesa',since:'da giugno',times:4,amt:'210,00'}],
 small:{count:356,total:'1.012,40',top:'Caffè, 262 volte',mix:[['coral',262],['mint',51],['sky',43]]},
 auto:{pct:'31%',amt:'5.566',vari:'12.389',subs:[['Affitto','3.840,00'],['Assicurazione auto','612,00'],['Netflix','167,88'],['Spotify','131,88'],['Telefono','119,88']],subsNoRent:'1.031,64'},
 streak:{days:287,of:365}
};

const storyTopY=(i,n)=>`<div class="story-top">${segs(i,n)}<div class="bar-row"><b>Il tuo ${Y.year}</b><span><span class="ib">${ms('close','','Chiudi')}</span></span></div></div>`;

function yearBars(w,h){
 const v=Y.months.v,n=12,gap=3,max=Math.max(...v),min=Math.min(...v),bw=(w-gap*(n-1))/n,top=20;let s='';
 v.forEach((m,k)=>{
  const x=k*(bw+gap),bh=Math.round(m/max*(h-top)),ext=m===max||m===min;
  s+=`<rect x="${x}" y="${h-bh}" width="${bw}" height="${bh}" fill="${m===max?'var(--fg)':'var(--bar)'}"/>`;
  if(m===min)s+=`<rect x="${x}" y="${h-bh}" width="${bw}" height="${bh}" fill="none" stroke="var(--fg)" stroke-width="2"/>`;
  if(ext)s+=`<text x="${x+bw/2}" y="${h-bh-6}" text-anchor="middle" font-size="12" font-weight="700" fill="var(--fg)">${m.toLocaleString('it-IT')}</text>`;
  s+=`<text x="${x+bw/2}" y="${h+16}" text-anchor="middle" font-size="12" font-weight="${ext?700:400}" fill="${ext?'var(--fg)':'var(--mute)'}">${Y.months.labels[k]}</text>`;
 });
 return `<svg class="chart" width="${w}" height="${h+22}" viewBox="0 0 ${w} ${h+22}" shape-rendering="crispEdges" role="img" aria-label="Spesa per mese nel 2026: agosto il più caro con 1.948 €, febbraio il più leggero con 1.198 €">${s}</svg>`;
}

function weekBars(w,h){
 const v=Y.week.v,n=7,gap=3,max=Math.max(...v),bw=(w-gap*(n-1))/n;let s='';
 v.forEach((p,k)=>{const x=k*(bw+gap),bh=Math.round(p/max*(h-20)),on=k===Y.week.top;
  s+=`<rect x="${x}" y="${h-bh}" width="${bw}" height="${bh}" fill="${on?'var(--fg)':'var(--bar)'}"/>`;
  s+=`<text x="${x+bw/2}" y="${h-bh-6}" text-anchor="middle" font-size="12" font-weight="${on?700:400}" fill="${on?'var(--fg)':'var(--mute)'}">${p}%</text>`;
  s+=`<text x="${x+bw/2}" y="${h+16}" text-anchor="middle" font-size="12" font-weight="${on?700:400}" fill="${on?'var(--fg)':'var(--mute)'}">${Y.week.labels[k]}</text>`});
 return `<svg class="chart" width="${w}" height="${h+22}" viewBox="0 0 ${w} ${h+22}" shape-rendering="crispEdges" role="img" aria-label="Quota delle spese per giorno della settimana: sabato 24%">${s}</svg>`;
}

function weeks52(w,h,missing,fill){
 const n=52,gap=2,bw=(w-gap*(n-1))/n;let s='';
 for(let k=0;k<n;k++){const x=k*(bw+gap),on=!missing.includes(k+1);
  s+=on?`<rect x="${x}" y="0" width="${bw}" height="${h}" fill="${fill}"/>`:`<rect x="${x}" y="${h-2}" width="${bw}" height="2" fill="var(--line)"/>`}
 return `<svg class="chart" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}" shape-rendering="crispEdges" role="img" aria-label="Caffè in 48 settimane su 52">${s}</svg>`;
}

function yearHeat(cell,gap){
 const lens=[31,28,31,30,31,30,31,31,30,31,30,31];let seed=7;const rnd=()=>(seed=(seed*9301+49297)%233280)/233280;
 let s=`<div role="img" aria-label="Spese registrate in 287 giorni su 365, un quadrato per giorno, un mese per riga" style="display:grid;grid-template-columns:14px repeat(31,${cell}px);gap:${gap}px;align-items:center">`;
 lens.forEach((len,m)=>{s+=`<span style="font-size:11px;color:var(--mute)">${Y.months.labels[m]}</span>`;
  for(let d=1;d<=31;d++){if(d>len){s+='<span></span>';continue}const on=rnd()>0.215;s+=`<span style="height:${cell}px;${on?'background:var(--fg)':'outline:1px solid var(--line);outline-offset:-1px'}"></span>`}});
 return s+'</div>';
}

function yearUnits(cell,gap,cols){
 let s=`<div role="img" aria-label="356 spese sotto i 5 €: 262 al bar, 51 di spesa, 43 di trasporti" style="display:grid;grid-template-columns:repeat(${cols},${cell}px);gap:${gap}px">`;
 Y.small.mix.forEach(([c,n])=>{for(let k=0;k<n;k++)s+=`<span style="height:${cell}px;background:var(--${c})"></span>`});
 return s+'</div>';
}
