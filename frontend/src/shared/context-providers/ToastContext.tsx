// // ToastProvider.tsx
// import { createContext, useContext, useRef, ReactNode } from 'react';
// import { Toast } from 'primereact/toast';

// interface ToastContextType {
//   show: (options: any) => void;  // or type it fully with Message
//   success: (msg: string) => void;
//   error: (msg: string) => void;
//   // add more helpers
// }

// const ToastContext = createContext<ToastContextType | undefined>(undefined);

// export const useToast = () => {
//   const ctx = useContext(ToastContext);
//   if (!ctx) throw new Error('useToast must be used within ToastProvider');
//   return ctx;
// };

// export function ToastProvider({ children }: { children: ReactNode }) {
//   const toastRef = useRef<any>(null);  // duck-type to avoid TS issues in alpha

//   const show = (options: any) => toastRef.current?.show(options);

//   const success = (detail: string) =>
//     show({ severity: 'success', summary: 'Success', detail });
//   const error = (detail: string) =>
//     show({ severity: 'error', summary: 'Error', detail });

//   return (
//     <ToastContext.Provider value={{ show, success, error /* etc */ }}>
//       <Toast ref={toastRef} position="top-right" />
//       {children}
//     </ToastContext.Provider>
//   );
// }
