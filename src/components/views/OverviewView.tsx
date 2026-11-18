import React from 'react';
import { SystemInfo, InventorySnapshot } from '../../types';
import {
  Monitor, Cpu, HardDrive, CalendarClock, ArrowRight, Eye,
  RefreshCw, Info, Laptop, CircuitBoard, Database, GitCompareArrows,
} from 'lucide-react';

interface OverviewViewProps {
  systemInfo: SystemInfo;
  inventoryHistory: InventorySnapshot[];
  isDarkMode: boolean;
  onNewInventory: () => void;
  onViewHistory: () => void;
  onCompare: () => void;
}

export const OverviewView: React.FC<OverviewViewProps> = ({
  systemInfo, inventoryHistory, isDarkMode, onNewInventory, onViewHistory, onCompare,
}) => {
  const panel = isDarkMode
    ? 'bg-slate-900 border-slate-800 text-slate-100'
    : 'bg-white border-slate-200 text-slate-800 shadow-sm';
  const muted = isDarkMode ? 'text-slate-400' : 'text-slate-500';
  const heading = isDarkMode ? 'text-slate-100' : 'text-slate-900';
  const subheading = 'text-[13px] font-semibold';
  const summary = [
    { label: 'Système d’exploitation', value: systemInfo.osName, detail: systemInfo.osVersion, icon: Monitor },
    { label: 'Processeur', value: systemInfo.processor, detail: 'Modèle déclaré dans les données de démonstration', icon: Cpu },
    { label: 'Mémoire installée', value: systemInfo.ramInstalled, detail: 'Capacité déclarée', icon: CircuitBoard },
    { label: 'Stockage', value: systemInfo.storageSummary, detail: 'Capacité déclarée', icon: HardDrive },
  ];

  return (
    <div className="mx-auto w-full max-w-[1440px] space-y-5 pb-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className={`text-xs font-medium ${muted}`}>ATLAS PC / ACCUEIL</div>
          <h2 className={`mt-1 text-[25px] font-semibold tracking-tight ${heading}`}>Vue d’ensemble</h2>
          <p className={`mt-1 text-sm ${muted}`}>Retrouvez les principales informations et les inventaires de cet ordinateur.</p>
        </div>
        <button onClick={onNewInventory} className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-blue-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-blue-800 focus-visible:outline-offset-2">
          <RefreshCw className="h-4 w-4" />
          Nouvel inventaire
        </button>
      </div>

      <div className={`flex items-start gap-3 rounded-md border px-4 py-3 ${isDarkMode ? 'border-amber-800/70 bg-amber-950/30 text-amber-200' : 'border-amber-200 bg-amber-50 text-amber-900'}`} role="status">
        <Info className="mt-0.5 h-[18px] w-[18px] shrink-0" />
        <div>
          <p className="text-sm font-semibold">Version de démonstration</p>
          <p className="mt-0.5 text-[13px] opacity-90">Les informations affichées ci-dessous sont des exemples. La collecte réelle du matériel et des logiciels n’est pas encore activée ; aucun nouvel inventaire réel ne peut être créé pour le moment.</p>
        </div>
      </div>

      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className={`${subheading} ${heading}`}>Configuration de l’ordinateur</h3>
          <span className={`text-xs ${muted}`}>Exemples de données</span>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
          {summary.map(({ label, value, detail, icon: Icon }) => (
            <article key={label} className={`min-w-0 rounded-md border p-4 ${panel}`}>
              <div className="flex items-center gap-2.5">
                <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-md ${isDarkMode ? 'bg-blue-950 text-blue-300' : 'bg-blue-50 text-blue-700'}`}><Icon className="h-[18px] w-[18px]" /></span>
                <span className={`text-[13px] font-medium ${muted}`}>{label}</span>
              </div>
              <p className={`mt-3 break-words text-[16px] font-semibold leading-snug ${heading}`} title={value}>{value}</p>
              <p className={`mt-1 text-xs ${muted}`}>{detail}</p>
            </article>
          ))}
        </div>
      </section>

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-[minmax(0,1.7fr)_minmax(280px,0.9fr)]">
        <section className={`min-w-0 overflow-hidden rounded-md border ${panel}`}>
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-inherit px-4 py-3.5">
            <div className="flex items-center gap-2.5">
              <CalendarClock className="h-[18px] w-[18px] text-blue-600" />
              <h3 className={subheading}>Historique des inventaires</h3>
            </div>
            <button onClick={onViewHistory} className="inline-flex items-center gap-1 text-[13px] font-semibold text-blue-600 hover:text-blue-700">
              Tout afficher <ArrowRight className="h-4 w-4" />
            </button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[620px] border-collapse text-left text-[13px]">
              <thead className={isDarkMode ? 'bg-slate-950/70 text-slate-400' : 'bg-slate-50 text-slate-500'}>
                <tr>
                  <th className="px-4 py-3 font-semibold">Date et heure</th>
                  <th className="px-4 py-3 font-semibold">Description</th>
                  <th className="px-4 py-3 font-semibold">Version</th>
                  <th className="px-4 py-3 font-semibold">Taille</th>
                  <th className="px-4 py-3 text-right font-semibold">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-inherit">
                {inventoryHistory.slice(0, 5).map((inv) => (
                  <tr key={inv.id} className={isDarkMode ? 'hover:bg-slate-800/60' : 'hover:bg-blue-50/60'}>
                    <td className="whitespace-nowrap px-4 py-3">{inv.date}</td>
                    <td className="px-4 py-3">{inv.description}</td>
                    <td className="whitespace-nowrap px-4 py-3">{inv.version}</td>
                    <td className="whitespace-nowrap px-4 py-3">{inv.totalSize}</td>
                    <td className="px-4 py-3 text-right">
                      <button onClick={onViewHistory} className="inline-flex items-center gap-1 rounded px-2 py-1 font-medium text-blue-600 hover:bg-blue-50/10" aria-label={`Consulter l’inventaire du ${inv.date}`}>
                        <Eye className="h-4 w-4" /> Consulter
                      </button>
                    </td>
                  </tr>
                ))}
                {inventoryHistory.length === 0 && <tr><td colSpan={5} className={`px-4 py-8 text-center ${muted}`}>Aucun inventaire enregistré.</td></tr>}
              </tbody>
            </table>
          </div>
          <div className={`flex flex-wrap items-center justify-between gap-3 border-t border-inherit px-4 py-3 text-xs ${muted}`}>
            <span>{inventoryHistory.length} entrée(s) d’exemple</span>
            <button onClick={onCompare} className="inline-flex items-center gap-1.5 font-semibold text-blue-600 hover:text-blue-700"><GitCompareArrows className="h-4 w-4" /> Comparer</button>
          </div>
        </section>

        <aside className="space-y-4">
          <section className={`rounded-md border p-4 ${panel}`}>
            <div className="mb-3 flex items-center gap-2.5">
              <Laptop className="h-[18px] w-[18px] text-blue-600" />
              <h3 className={subheading}>Identification</h3>
            </div>
            <dl className="space-y-3 text-[13px]">
              {[
                ['Nom du PC', systemInfo.computerName], ['Fabricant', systemInfo.manufacturer],
                ['Modèle', systemInfo.model], ['Architecture', systemInfo.architecture],
                ['Résolution', systemInfo.screenResolution],
              ].map(([label, value]) => <div key={label} className="flex items-start justify-between gap-3 border-b border-inherit pb-2 last:border-0 last:pb-0"><dt className={muted}>{label}</dt><dd className="max-w-[62%] break-words text-right font-medium">{value}</dd></div>)}
            </dl>
          </section>
          <section className={`rounded-md border p-4 ${panel}`}>
            <div className="flex items-center gap-2.5">
              <Database className="h-[18px] w-[18px] text-blue-600" />
              <h3 className={subheading}>À propos des données</h3>
            </div>
            <p className={`mt-2 text-[13px] leading-relaxed ${muted}`}>Les exemples permettent de découvrir la présentation d’ATLAS PC. Ils ne décrivent pas nécessairement la configuration de votre ordinateur et ne doivent pas être utilisés comme référence technique.</p>
          </section>
        </aside>
      </div>
    </div>
  );
};
