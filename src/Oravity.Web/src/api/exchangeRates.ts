import apiClient from './client';

export interface ExchangeRateResponse {
  fromCurrency: string;
  toCurrency: string;
  rate: number;
  rateDate: string;
}

export const exchangeRatesApi = {
  getCurrent: (currency: string, date?: string) =>
    apiClient.get<ExchangeRateResponse>('/exchange-rates/current', {
      params: { currency, ...(date && { date }) },
    }),
};
