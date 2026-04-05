import apiClient from './client';

export interface GeoItem {
  id: number;
  name: string;
}

export interface CountryItem extends GeoItem {
  isoCode: string;
}

export interface NationalityItem extends GeoItem {
  code: string;
}

export const geoApi = {
  getCountries: () =>
    apiClient.get<CountryItem[]>('/geo/countries'),

  getCities: (countryId: number) =>
    apiClient.get<GeoItem[]>('/geo/cities', { params: { countryId } }),

  getDistricts: (cityId: number) =>
    apiClient.get<GeoItem[]>('/geo/districts', { params: { cityId } }),

  getNationalities: () =>
    apiClient.get<NationalityItem[]>('/geo/nationalities'),
};
