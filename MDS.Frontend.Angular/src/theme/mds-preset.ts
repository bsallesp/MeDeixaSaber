import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'

const MdsPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50:'{indigo.50}',100:'{indigo.100}',200:'{indigo.200}',300:'{indigo.300}',
      400:'{indigo.400}',500:'{indigo.500}',600:'{indigo.600}',700:'{indigo.700}',
      800:'{indigo.800}',900:'{indigo.900}',950:'{indigo.950}'
    },
    neutral: {
      50:'{zinc.50}',100:'{zinc.100}',200:'{zinc.200}',300:'{zinc.300}',
      400:'{zinc.400}',500:'{zinc.500}',600:'{zinc.600}',700:'{zinc.700}',
      800:'{zinc.800}',900:'{zinc.900}',950:'{zinc.950}'
    },
    info: {
      50:'{sky.50}',100:'{sky.100}',200:'{sky.200}',300:'{sky.300}',
      400:'{sky.400}',500:'{sky.500}',600:'{sky.600}',700:'{sky.700}',
      800:'{sky.800}',900:'{sky.900}',950:'{sky.950}'
    },
    success: {
      50:'{emerald.50}',100:'{emerald.100}',200:'{emerald.200}',300:'{emerald.300}',
      400:'{emerald.400}',500:'{emerald.500}',600:'{emerald.600}',700:'{emerald.700}',
      800:'{emerald.800}',900:'{emerald.900}',950:'{emerald.950}'
    },
    warn: {
      50:'{amber.50}',100:'{amber.100}',200:'{amber.200}',300:'{amber.300}',
      400:'{amber.400}',500:'{amber.500}',600:'{amber.600}',700:'{amber.700}',
      800:'{amber.800}',900:'{amber.900}',950:'{amber.950}'
    },
    danger: {
      50:'{rose.50}',100:'{rose.100}',200:'{rose.200}',300:'{rose.300}',
      400:'{rose.400}',500:'{rose.500}',600:'{rose.600}',700:'{rose.700}',
      800:'{rose.800}',900:'{rose.900}',950:'{rose.950}'
    }
  },

  surface: {
    0:  '{zinc.50}',
    50: '{zinc.100}',
    100:'{zinc.200}',
    200:'{zinc.300}',
    300:'{zinc.400}',
    400:'{zinc.500}',
    500:'{zinc.600}',
    600:'{zinc.700}',
    700:'{zinc.800}',
    800:'{zinc.900}',
    900:'{zinc.950}',
    950:'{black}'
  },

  content: {
    text: {
      color: '{neutral.900}',
      secondary: '{neutral.700}',
      muted: '{neutral.500}',
      inverted: '{white}'
    }
  },

  typography: {
    fontFamily: 'Inter, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
    fontSize: '1rem',
    lineHeight: '1.6'
  },

  radius: { container: '12px', content: '10px', control: '10px' },
  focus: { ringWidth: '2px', ringOffset: '2px', ringColor: '{primary.500}' },
  states: { hoverDarker: true, transitionDuration: '120ms' }
})

export default MdsPreset
