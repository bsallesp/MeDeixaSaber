import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'

const MyPreset = definePreset(Aura, {
  typography: {
    fontFamily: 'Inter, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
    fontSize: '1rem',
    fontWeight: '400'
  }
})

export default MyPreset
