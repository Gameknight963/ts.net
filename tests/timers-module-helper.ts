export const events: string[] = [];
export const value = await new Promise<number>(resolve => setTimeout(() => { events.push('timer'); resolve(12); }, 1));
events.push('initialized');
