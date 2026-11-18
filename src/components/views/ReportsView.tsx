import React from 'react';
import { BarChart3, Download, Printer } from 'lucide-react';

interface ReportsViewProps {
  isDarkMode: boolean;
}

export const ReportsView: React.FC<ReportsViewProps> = ({ isDarkMode }) => {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold">Rapports système et exports</h2>
          <p className="text-xs opacity-75 mt-0.5">Générez des rapports complets au format texte ou PDF de votre configuration.</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => alert('Rapport exporté avec succès.')}
            className="flex items-center gap-2 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold shadow-md transition-all active:scale-95"
          >
            <Download className="w-4 h-4" />
            <span>Exporter le rapport</span>
          </button>
        </div>
      </div>

      <div className={`p-6 rounded-xl border space-y-4 ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-600/10 text-blue-500 flex items-center justify-center">
            <BarChart3 className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold">Rapport d'inventaire complet v1.0.0</h3>
            <p className="text-xs opacity-75">Généré le 18/11/2026 à 16:45 — PC-ATLAS-WORKSTATION</p>
          </div>
        </div>

        <hr className="border-inherit" />

        <div className="space-y-2 text-xs font-mono">
          <div className="flex justify-between py-1">
            <span className="opacity-75">Système d'exploitation :</span>
            <span>Windows 11 Professionnel 23H2</span>
          </div>
          <div className="flex justify-between py-1">
            <span className="opacity-75">Processeur :</span>
            <span>Intel Core i7-13700K</span>
          </div>
          <div className="flex justify-between py-1">
            <span className="opacity-75">Mémoire RAM :</span>
            <span>32,0 Go DDR5</span>
          </div>
          <div className="flex justify-between py-1">
            <span className="opacity-75">Applications installées :</span>
            <span>48 logiciels répertoriés</span>
          </div>
        </div>
      </div>
    </div>
  );
};
