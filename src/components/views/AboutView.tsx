import React from 'react';
import { Info, ShieldCheck } from 'lucide-react';

interface AboutViewProps {
  isDarkMode: boolean;
}

export const AboutView: React.FC<AboutViewProps> = ({ isDarkMode }) => {
  return (
    <div className="space-y-6 max-w-2xl mx-auto py-6">
      <div className={`p-8 rounded-2xl border text-center space-y-4 ${isDarkMode ? 'bg-slate-900/90 border-slate-800' : 'bg-white border-slate-300 shadow-lg'}`}>
        <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-blue-600 to-cyan-600 flex items-center justify-center text-white text-2xl font-extrabold mx-auto shadow-lg">
          A
        </div>

        <div>
          <h2 className="text-2xl font-extrabold">ATLAS PC</h2>
          <p className="text-xs font-mono text-blue-500 mt-1">Version 1.0.0 (Build 20261118)</p>
        </div>

        <p className="text-xs opacity-80 leading-relaxed">
          Logiciel professionnel d'inventaire, de consultation et de suivi d'évolution des configurations matérielles et logicielles sous Windows.
        </p>

        <div className="flex items-center justify-center gap-2 text-xs text-emerald-500 font-medium bg-emerald-500/10 border border-emerald-500/20 px-3 py-1.5 rounded-full w-fit mx-auto">
          <ShieldCheck className="w-4 h-4" />
          <span>Logiciel vérifié et sécurisé sans publicité</span>
        </div>

        <div className="pt-4 border-t border-inherit text-[11px] opacity-60 font-mono">
          © 2026 ATLAS PC Team. Tous droits réservés.
        </div>
      </div>
    </div>
  );
};
