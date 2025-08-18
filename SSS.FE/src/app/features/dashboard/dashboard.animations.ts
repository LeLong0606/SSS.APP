import { trigger, state, style, transition, animate, query, stagger, keyframes } from '@angular/animations';

// Dashboard Animations
export const dashboardAnimations = [
  // Card slide in animation
  trigger('cardSlideIn', [
    transition(':enter', [
      style({ opacity: 0, transform: 'translateY(30px)' }),
      animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
    ])
  ]),

  // Counter animation
  trigger('counterAnimation', [
    transition(':enter', [
      query('.stat-number', [
        style({ opacity: 0, transform: 'scale(0.8)' }),
        stagger(100, [
          animate('600ms cubic-bezier(0.35, 0, 0.25, 1)', style({ opacity: 1, transform: 'scale(1)' }))
        ])
      ], { optional: true })
    ])
  ]),

  // List animation
  trigger('listAnimation', [
    transition('* => *', [
      query(':enter', [
        style({ opacity: 0, transform: 'translateX(-20px)' }),
        stagger(50, [
          animate('300ms ease-out', style({ opacity: 1, transform: 'translateX(0)' }))
        ])
      ], { optional: true })
    ])
  ]),

  // Action hover animation
  trigger('actionHover', [
    state('idle', style({ transform: 'scale(1)' })),
    state('hover', style({ transform: 'scale(1.02)' })),
    transition('idle <=> hover', animate('200ms ease-in-out'))
  ]),

  // Fade in animation
  trigger('fadeIn', [
    transition(':enter', [
      style({ opacity: 0 }),
      animate('300ms ease-in', style({ opacity: 1 }))
    ]),
    transition(':leave', [
      animate('300ms ease-out', style({ opacity: 0 }))
    ])
  ]),

  // Slide toggle animation
  trigger('slideToggle', [
    transition(':enter', [
      style({ height: '0px', overflow: 'hidden' }),
      animate('300ms ease-in-out', style({ height: '*' }))
    ]),
    transition(':leave', [
      style({ height: '*', overflow: 'hidden' }),
      animate('300ms ease-in-out', style({ height: '0px' }))
    ])
  ]),

  // Pulse animation for loading states
  trigger('pulse', [
    state('active', style({ transform: 'scale(1.05)' })),
    state('inactive', style({ transform: 'scale(1)' })),
    transition('active <=> inactive', animate('1s ease-in-out'))
  ]),

  // Rotate animation for refresh button
  trigger('rotate', [
    state('false', style({ transform: 'rotate(0deg)' })),
    state('true', style({ transform: 'rotate(360deg)' })),
    transition('false => true', animate('500ms ease-in-out')),
    transition('true => false', animate('500ms ease-in-out'))
  ])
];
